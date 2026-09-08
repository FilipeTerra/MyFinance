"""
import_kb_to_api.py — Migração one-off do knowledge_base.json para a API.

Até a importação de extrato passar para a camada da API, o aprendizado
"descrição → categoria" vivia neste microsserviço, num JSON local chaveado por
conta. Agora ele mora na tabela CategoryRules do banco da API, chaveado por
usuário — assim continua disponível com o agente de IA fora do ar.

Este script NÃO se conecta ao banco: ele gera um arquivo .sql para você revisar
e aplicar. O mapeamento conta → usuário e categoria → id é resolvido pelo próprio
SQL, então regras que apontam para categorias já excluídas simplesmente não são
inseridas. O INSERT é idempotente (ON CONFLICT DO NOTHING), então rodar duas
vezes não duplica nada.

Uso (a partir de qualquer diretório):
    python scripts/import_kb_to_api.py                      # gera knowledge_base_rules.sql
    python scripts/import_kb_to_api.py --out /tmp/regras.sql
    python scripts/import_kb_to_api.py --dry-run            # só mostra o resumo

Depois de aplicar o SQL e conferir as regras no banco, o knowledge_base.json
pode ser apagado.
"""
import argparse
import json
import os
import re
import sys
import unicodedata
from typing import Dict, List, Tuple

_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(_ROOT)
sys.path.insert(0, _ROOT)

_DEFAULT_INPUT = "knowledge_base.json"
_DEFAULT_OUTPUT = "knowledge_base_rules.sql"

# ── Normalização ──────────────────────────────────────────────────────────────
# Porte fiel de StatementTextNormalizer.Normalize (C#). As duas precisam produzir
# exatamente a mesma chave: é ela que liga o que foi aprendido ao que será lido
# no próximo extrato. Se mexer em uma, mexa na outra.

_COUNTRY_SUFFIXES = {"BRA", "BR", "USA", "US", "ARG", "PRT", "ESP", "GBR"}
_INSTALLMENT_RE = re.compile(r"\(PARCELA\s+\d+\s+DE\s+\d+\)|\bPARCELA\s+\d+\s*[/DE]+\s*\d+\b")
_NON_ALNUM_RE = re.compile(r"[^A-Z0-9 ]")
_EXTRA_SPACES_RE = re.compile(r"\s{2,}")


def normalize(description: str) -> str:
    if not description or not description.strip():
        return ""

    decomposed = unicodedata.normalize("NFD", description)
    text = "".join(c for c in decomposed if unicodedata.category(c) != "Mn").upper()

    text = _INSTALLMENT_RE.sub(" ", text)
    text = _NON_ALNUM_RE.sub(" ", text)
    text = _EXTRA_SPACES_RE.sub(" ", text).strip()

    tokens = [t for t in text.split(" ") if t and not (len(t) >= 4 and t.isdigit())]

    while len(tokens) > 1 and tokens[-1] in _COUNTRY_SUFFIXES:
        tokens.pop()

    return " ".join(tokens)


# ── Geração do SQL ────────────────────────────────────────────────────────────


def _quote(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def build_statements(memory: Dict[str, Dict[str, str]]) -> Tuple[List[str], int]:
    """Devolve os comandos SQL e quantas regras eles cobrem."""
    statements: List[str] = []
    converted = 0

    for account_id, rules in memory.items():
        # Chaves distintas do JSON podem colidir após a normalização (a antiga só
        # minusculava). Fica a última — igual ao comportamento da API ao aprender.
        by_key: Dict[str, str] = {}

        for description, category_name in rules.items():
            key = normalize(description)
            if not key or not category_name or not category_name.strip():
                continue
            by_key[key] = category_name.strip()

        for key, category_name in sorted(by_key.items()):
            converted += 1
            statements.append(
                'INSERT INTO "CategoryRules" ("Id", "UserId", "DescriptionKey", "CategoryId", "CreatedAt", "UpdatedAt")\n'
                '    SELECT gen_random_uuid(), a."UserId", {key}, c."Id", now(), now()\n'
                '      FROM "Accounts" a\n'
                '      JOIN "Categories" c ON c."UserId" = a."UserId" AND lower(c."Name") = lower({category})\n'
                '     WHERE a."Id" = {account}::uuid\n'
                'ON CONFLICT ("UserId", "DescriptionKey") DO NOTHING;'.format(
                    key=_quote(key),
                    category=_quote(category_name),
                    account=_quote(account_id),
                )
            )

    return statements, converted


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", default=_DEFAULT_INPUT, help="knowledge_base.json de origem")
    parser.add_argument("--out", default=_DEFAULT_OUTPUT, help="arquivo .sql de destino")
    parser.add_argument("--dry-run", action="store_true", help="não escreve o arquivo, só resume")
    args = parser.parse_args()

    if not os.path.exists(args.input):
        print(f"Arquivo não encontrado: {args.input}")
        return 1

    with open(args.input, "r", encoding="utf-8") as fh:
        memory = json.load(fh)

    total = sum(len(rules) for rules in memory.values())
    statements, converted = build_statements(memory)
    discarded = total - converted

    print(f"Contas no arquivo ......: {len(memory)}")
    print(f"Regras no arquivo ......: {total}")
    print(f"Regras a inserir .......: {converted}")
    print(f"Regras descartadas .....: {discarded} (descrição vazia ou colisão após normalização)")

    if args.dry_run:
        return 0

    header = (
        "-- Gerado por scripts/import_kb_to_api.py — migração do knowledge_base.json\n"
        "-- para a tabela CategoryRules. Idempotente: pode rodar mais de uma vez.\n"
        "-- Regras cuja categoria não existe mais no usuário simplesmente não entram.\n"
        "BEGIN;\n\n"
    )

    with open(args.out, "w", encoding="utf-8") as fh:
        fh.write(header)
        fh.write("\n\n".join(statements))
        fh.write("\n\nCOMMIT;\n")

    print(f"\nSQL gravado em: {args.out}")
    print("Aplique com:  psql \"$ConnectionString\" -f " + args.out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
