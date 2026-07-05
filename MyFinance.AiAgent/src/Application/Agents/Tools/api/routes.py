"""
routes.py — Rotas do backend .NET usadas pelas tools do agente.

Centraliza os paths num único lugar — incluindo o casing inconsistente do
próprio .NET (ex: /Profile com P maiúsculo, /investimentos em português
minúsculo, o resto em inglês minúsculo) — para que uma mudança de contrato
do backend não exija caçar strings espalhadas pelos módulos de domínio.
"""
ACCOUNTS = "/accounts"
ACCOUNT_TRANSACTIONS = "/transactions/account/{account_id}"
TRANSACTIONS = "/transactions"
FINANCIAL_GOALS = "/financial-goals"
CATEGORIES = "/categories"
PROFILE = "/Profile"
INVESTMENTS = "/investimentos"
