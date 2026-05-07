# TODO — TGE Launcher

Récapitulatif des améliorations possibles, classées par priorité.

---

## 🔴 Critique (sécurité / fiabilité)

- [ ] **Chiffrer credentials.json** via DPAPI (`ProtectedData.Protect`, `DataProtectionScope.CurrentUser`) — actuellement mot de passe en clair dans [LauncherSrc/Program.cs:1079-1087](LauncherSrc/Program.cs#L1079-L1087).
- [ ] **Passer `Main` en `async Task`** et supprimer les `.Result` / `.GetAwaiter().GetResult()` ([Program.cs:32](LauncherSrc/Program.cs#L32), [245](LauncherSrc/Program.cs#L245), [1185](LauncherSrc/Program.cs#L1185), [1227-1229](LauncherSrc/Program.cs#L1227-L1229)) — risque de deadlock.
- [ ] **`HttpClient` en singleton `static readonly`** au lieu d'une instance par appel ([Program.cs:235](LauncherSrc/Program.cs#L235), [1121](LauncherSrc/Program.cs#L1121), [1180](LauncherSrc/Program.cs#L1180)) — évite l'épuisement de sockets.
- [ ] **Timeout sur `GetLogiciel()`** ([Program.cs:388-410](LauncherSrc/Program.cs#L388-L410)) et sur la boucle principale ([Program.cs:78-99](LauncherSrc/Program.cs#L78-L99)) — actuellement infinies si TrucksBook ne démarre jamais.
- [ ] **Parser le JSON proprement** dans `CheckLoginRemote` au lieu de `body.Contains("\"status\":\"success\"")` ([Program.cs:248](LauncherSrc/Program.cs#L248)).
- [ ] **Sécuriser `Updater.bat`** : vérifier le download avant de supprimer les `*.exe`/`*.zip` ([Updater.bat:10-11](LauncherSrc/tomove/Updater.bat#L10-L11)) — ajouter un rollback.

## 🟠 Architecture / qualité de code

- [ ] **Découper `Program.cs` (1244 lignes)** en classes : `TrucksBookController`, `ModInstaller`, `Ets2Locator`, `LoginForm`, `UpdateService`.
- [ ] **Supprimer les `#pragma warning disable CS8602/8600/8603`** ([Program.cs:1-3](LauncherSrc/Program.cs#L1-L3)) et corriger les vraies issues de nullabilité.
- [ ] **Introduire des interfaces injectables** (`ITrucksBookClient`, `IFileSystem`, `IRegistry`) pour rendre le code testable — actuellement tout est `static`.
- [ ] **Centraliser les magic strings** (`"TB Client"`, `"btnSpustitEts2"`, `"txtPass"`, `"txtEmail"`, `@"Software\TrucksBook"`, `"tge_mod_*.zip"`) dans une classe `Constants`.
- [ ] **Disposer le `Process` retourné par `GetLogiciel()`** ([Program.cs:134](LauncherSrc/Program.cs#L134), [158](LauncherSrc/Program.cs#L158), [260](LauncherSrc/Program.cs#L260)) — fuite de handle.
- [ ] **Extraire `SauvegarderIdentifiants()` (360 lignes WinForms)** ([Program.cs:669-1026](LauncherSrc/Program.cs#L669-L1026)) dans une classe `LoginForm : Form` (Designer ou migration WPF/Avalonia).

## 🟡 Performance

- [ ] **Cacher le chemin de `eurotrucks2.exe` et du dossier `mod`** dans le registre/AppData — actuellement scan complet de tous les disques ([Program.cs:511-533](LauncherSrc/Program.cs#L511-L533), [630-647](LauncherSrc/Program.cs#L630-L647)).
- [ ] **Backoff dans le polling** ([Program.cs:404](LauncherSrc/Program.cs#L404)) — actuellement 100 ms en boucle + log spammé.
- [ ] **`process.WaitForExit(2000)`** au lieu de `Thread.Sleep(500)` après `Kill()` ([Program.cs:46](LauncherSrc/Program.cs#L46), [56](LauncherSrc/Program.cs#L56)).

## 🟢 Fonctionnalités

- [ ] **Barre de progression** pour le download du launcher/mod ([DownloadLatestModVersion](LauncherSrc/Program.cs#L1176-L1240)).
- [ ] **Tray icon** pour rester accessible après la minimisation de TrucksBook.
- [ ] **Logs structurés multi-niveaux** (Info/Warn/Error) — actuellement seuls les exceptions sont loggées ([ErrorLogger.cs](LauncherSrc/ErrorLogger.cs)).
- [ ] **Rotation des logs** — [ErrorLogger.cs:67-68](LauncherSrc/ErrorLogger.cs#L67-L68) ne purge jamais les anciens fichiers.
- [ ] **Bouton "Voir les logs"** dans le formulaire ou la tray.
- [ ] **Vérification d'intégrité** (SHA256) du zip téléchargé.
- [ ] **Politique de retry** (3 tentatives + backoff exponentiel) sur les appels réseau.
- [ ] **i18n via `.resx`** si une version EN est envisagée — toutes les strings sont actuellement en français.
- [ ] **Détection de conflit de mods** ETS2 actifs avant d'installer TGE-MOD.

## 🔵 Build / DevOps

- [ ] **GitHub Actions** : automatiser `dotnet publish` + tests + release sur tag `v*`.

- [ ] **Inverser l'ordre tests/publish** dans [LauncherCompile.bat:39](LauncherCompile.bat#L39) — tester avant de publier.
- [ ] **Documenter `tests-credentials.json`** — sans ce fichier les tests d'intégration FlaUI sont silencieusement skipped.

## 🧪 Tests

- [ ] **Couvrir le flot principal** (registre, installation mod, login UIA, recherche `eurotrucks2.exe`) — seuls `Credentials`, `VersionParsing`, `PathHelpers`, `ErrorLogger` sont testés. Nécessite la refacto vers classes injectables.
