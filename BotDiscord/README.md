# Bot Discord en Python

Ce projet contient un bot Discord minimal en Python.

Prérequis
- Python 3.8+
- Un token de bot Discord

Installation (PowerShell)

```powershell
python -m pip install -r requirements.txt
cp .env.example .env
# Éditez .env et mettez DISCORD_TOKEN
python bot.py
```

Commandes disponibles
- `!ping` : répond Pong! et affiche la latence

Tests

```powershell
python -m pytest -q
```

Note
Assurez-vous d'activer l'intent "Message Content" dans le tableau de bord Discord Developer pour que la commande `!ping` fonctionne.
