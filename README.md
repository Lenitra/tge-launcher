<div align="center">

<img src="LauncherSrc/assets/logo.png" alt="TGE Launcher" width="140" />

# TGE Launcher

**Le launcher officiel de la compagnie virtuelle TGE pour Euro Truck Simulator 2.**

Un clic, et c'est parti : le mod TGE est à jour, TrucksBook est configuré, ETS2 démarre.

[![Télécharger](https://img.shields.io/github/v/release/Lenitra/tge-launcher?label=T%C3%A9l%C3%A9charger&style=for-the-badge&color=f5a623)](https://github.com/Lenitra/tge-launcher/releases/latest)
[![Discord](https://img.shields.io/badge/Discord-Rejoindre-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/7yTzWATs66)
[![Plateforme](https://img.shields.io/badge/Windows-10%2F11-0078D4?style=for-the-badge&logo=windows&logoColor=white)](#)

</div>

---

## ✨ Pourquoi TGE Launcher ?

Lancer une session de convoi avec la compagnie TGE, c'était jusque-là une succession de manipulations : ouvrir TrucksBook, se connecter, vérifier le chemin d'ETS2, télécharger le bon mod, copier les fichiers dans le bon dossier, lancer ETS2…

**Le TGE Launcher fait tout pour vous, en un seul clic.**

## 🚛 Fonctionnalités

- 🔄 **Mise à jour automatique du mod TGE** — récupère la dernière version depuis GitHub.
- 📦 **Installation propre** — supprime les anciennes versions du mod avant d'installer la nouvelle.
- 🔍 **Détection automatique d'ETS2** — trouve l'exécutable même si Steam est installé sur un autre disque.
- 🔐 **Connexion automatique à TrucksBook** — vos identifiants sont mémorisés localement.
- 🎮 **Lancement d'ETS2** — TrucksBook se réduit, le jeu démarre, vous roulez.
- 🛠️ **Auto-update du launcher** — la prochaine version se télécharge et s'installe toute seule.

## 📥 Installation

1. Téléchargez la dernière version sur la [page des releases](https://github.com/Lenitra/tge-launcher/releases/latest) (fichier `LauncherTGE_xxxxxxxxx.zip`).
2. Décompressez l'archive où vous voulez (par ex. `C:\Games\TGE-Launcher`).
3. Double-cliquez sur `LauncherSrc.exe`.

> [!IMPORTANT]
> Le launcher nécessite **Euro Truck Simulator 2** déjà installé via Steam et un compte **[TrucksBook](https://trucksbook.eu)**.

## 🎯 Utilisation

1. Lancez le launcher.
2. Au premier démarrage, **saisissez vos identifiants TrucksBook** (email + mot de passe).
3. Cliquez sur **JOUER**.
4. Le launcher s'occupe du reste : mise à jour du mod, configuration de TrucksBook, lancement d'ETS2.

Pas encore de compte TrucksBook ? Le bouton **S'INSCRIRE** vous emmène directement sur la page d'inscription.

## 💬 Communauté

Vous voulez rejoindre la compagnie, poser une question, ou signaler un bug ?

👉 **[Rejoignez le Discord TGE](https://discord.gg/7yTzWATs66)**

## 🐛 Signaler un problème

- Un bug ? Ouvrez une [issue GitHub](https://github.com/Lenitra/tge-launcher/issues/new) ou parlez-en sur le Discord.
- Les logs d'erreur sont stockés dans le sous-dossier `logs/` à côté de l'exécutable — joignez-les à votre rapport, ça aide énormément.

## 🔧 Compiler depuis les sources

Pour les développeurs ou les curieux :

**Prérequis**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10 (build 17763) ou supérieur

**Build**

```bat
git clone https://github.com/Lenitra/tge-launcher.git
cd tge-launcher
LauncherCompile.bat
```

Le script :
1. Compile le launcher en single-file self-contained (`win-x64`)
2. Empaquette le mod TGE et TrucksBook
3. Lance les tests xUnit
4. Génère l'archive finale dans `build\v<version>\LauncherTGE_<version>.zip`

**Tests d'intégration FlaUI (optionnel)**

Créez un fichier `tests-credentials.json` à la racine avec un compte TrucksBook de test :

```json
{ "Email": "test@example.com", "Password": "votre-mdp-de-test" }
```

Sans ce fichier, les tests d'intégration sont automatiquement ignorés.

## 🧱 Stack technique

- **.NET 9** + **WinForms** pour l'UI
- **[FlaUI](https://github.com/FlaUI/FlaUI)** (UIA3) pour automatiser TrucksBook
- **xUnit** pour les tests
- API GitHub Releases pour la distribution et l'auto-update

## 📜 Crédits

- Développé par [@Lenitra](https://github.com/Lenitra) pour la compagnie virtuelle **TGE**.
- Utilise [TrucksBook](https://trucksbook.eu) — merci à leur équipe pour l'API.
