#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600// Dereference of a possibly null reference.
#pragma warning disable CS8603// Dereference of a possibly null reference.
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Win32;

public class Program
{
    static void Main(string[] args)
    {

        DownloadLatestModVersion();
        RunInstallUpdate();


        Thread.Sleep(1000000);
    }

    static void a(string[] args)
    {
        Console.WriteLine("Lancement du launcher...");
        // Initialiser WinForms rendering settings AVANT toute création de fenêtre WinForms
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);


        bool etsStarted = false;
        KillTrucksBook();
        Thread.Sleep(500); // Attendre un peu pour s'assurer que le processus est bien terminé

        // CheckAndUpdateLauncher();

        if (!ETS2RegistryKeyExist())
        {
            Console.WriteLine("Premier lancement...");
            StartTrucksBook();
            Thread.Sleep(2000);
        }

        KillTrucksBook();
        Thread.Sleep(500); // Attendre un peu pour s'assurer que le processus est bien terminé

        Console.WriteLine("Installation du mod et configuration automatique du chemin ETS2...");
        InstallMod();

        ModifierCheminETS2(FindEuroTrucks2Exe() ?? string.Empty);

        while (CheckLogin() == false || GetCredentials() == null || GetCredentials().Email == string.Empty || GetCredentials().Password == string.Empty)
        {
            SauvegarderIdentifiants();
        }

        Console.WriteLine("Démarrage de TrucksBook...");
        StartTrucksBook();


        while (!etsStarted)
        {
            ModifierCheminETS2(FindEuroTrucks2Exe() ?? string.Empty);
            if (DetectPageOnTrucksBook() == "login")
            {
                LoginToTrucksBook();
            }

            if (DetectPageOnTrucksBook() == "home")
            {
                Thread.Sleep(500);
                StartETS2();
                etsStarted = true;
            }

        }

        Console.WriteLine("ETS2 a été lancé avec succès.");
        // Réduire la fenêtre de TrucksBook
        MinimizeTrucksBook();
    }



    #region TrucksBook 
    static void StartTrucksBook()
    {
        string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrucksBook", "TB Client.exe");
        exePath = Path.GetFullPath(exePath);
        if (File.Exists(exePath))
        {
            try
            {
                using var proc = Process.Start(exePath);
                Console.WriteLine($"Lancement de TrucksBook depuis : {exePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du lancement de TrucksBook : {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Fichier introuvable : {exePath}");
        }
    }

    static void KillTrucksBook()
    {
        var logiciel = GetLogiciel(false);
        if (logiciel != null)
        {
            try
            {
                logiciel.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la fermeture de TrucksBook : {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Processus TrucksBook non trouvé.");
        }
    }

    static void LoginToTrucksBook()
    {
        string email = string.Empty;
        string password = string.Empty;

        var logiciel = GetLogiciel();
        if (logiciel == null)
        {
            Console.WriteLine("Processus TrucksBook non trouvé.");
            return;
        }

        // Lire les identifiants depuis credentials.json
        Credentials? creds = GetCredentials();
        if (creds == null)
        {
            Console.WriteLine("Identifiants invalides dans credentials.json.");
            return;
        }

        email = creds.Email;
        password = creds.Password;

        try
        {
            using var automation = new UIA3Automation();
            using var app = FlaUI.Core.Application.Attach(logiciel);
            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return;
            }

            var emailBox = window.FindFirstDescendant(cf => cf.ByAutomationId("txtEmail"))?.AsTextBox();
            var passwordBox = window.FindFirstDescendant(cf => cf.ByAutomationId("txtPass"))?.AsTextBox();
            var loginButton = window.FindFirstDescendant(cf => cf.ByAutomationId("btnLogin"))?.AsButton();

            if (emailBox == null || passwordBox == null || loginButton == null)
            {
                Console.WriteLine("Un ou plusieurs éléments de connexion sont introuvables (txtEmail, txtPass, btnLogin).");
                return;
            }

            emailBox.Enter(email);

            passwordBox.Focus();
            passwordBox.Text = "";
            passwordBox.Enter(password);

            loginButton.Click();
            Console.WriteLine("Clic sur le bouton de connexion effectué.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la connexion: {ex.Message}");
        }
    }

    static bool CheckLogin()
    {
        // Lire les identifiants depuis credentials.json
        Credentials? creds = GetCredentials();
        if (creds == null || string.IsNullOrWhiteSpace(creds.Email) || string.IsNullOrWhiteSpace(creds.Password))
        {
            Console.WriteLine("Identifiants invalides dans credentials.json.");
            return false;
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("tge-launcher");

            var url = "https://trucksbook.eu/login-user";
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("email", creds.Email),
                new KeyValuePair<string, string>("pass", creds.Password),
            });

            using var response = client.PostAsync(url, form).GetAwaiter().GetResult();
            bool ok = response.StatusCode == HttpStatusCode.OK;
            Console.WriteLine($"Login check HTTP {(int)response.StatusCode} {(response.ReasonPhrase ?? string.Empty)} -> {(ok ? "OK" : "NOK")}");
            return ok;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la vérification du login: {ex.Message}");
            return false;
        }
    }

    static string DetectPageOnTrucksBook()
    {
        var logiciel = GetLogiciel();
        using (var automation = new UIA3Automation())
        using (var app = FlaUI.Core.Application.Attach(logiciel))
        {
            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return "null";
            }

            var homeButton = window.FindFirstDescendant(cf => cf.ByAutomationId("btnSpustitEts2"))?.AsButton();
            if (homeButton != null)
            {
                return "home";
            }

            var inputLogin = window.FindFirstDescendant(cf => cf.ByAutomationId("txtPass"))?.AsTextBox();
            if (inputLogin != null)
            {
                return "login";
            }
        }
        return "autre";
    }


    static void MinimizeTrucksBook()
    {
        var logiciel = GetLogiciel();
        using (var automation = new UIA3Automation())
        using (var app = FlaUI.Core.Application.Attach(logiciel))
        {
            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return;
            }

            // Réduire la fenêtre
            var windowPattern = window.Patterns.Window.Pattern;
            windowPattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Minimized);
            Console.WriteLine("Fenêtre de TrucksBook réduite.");
        }
    }

    static void StartETS2()
    {
        var logiciel = GetLogiciel();
        using (var automation = new UIA3Automation())
        using (var app = FlaUI.Core.Application.Attach(logiciel))
        {
            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return;
            }

            var startButton = window.FindFirstDescendant(cf => cf.ByAutomationId("btnSpustitEts2"))?.AsButton();
            if (startButton == null)
            {
                Console.WriteLine("Bouton de lancement ETS2 non trouvé (AutomationId: 'btnSpustitEts2').");
                return;
            }

            startButton.Focus();
            startButton.Click();
            Console.WriteLine("Clic sur le bouton de lancement ETS2 effectué.");
        }
    }


    public static bool ETS2RegistryKeyExist()
    {
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\TrucksBook", writable: true);
        if (key != null)
        {
            return true;
        }
        return false;
    }

    public static void ModifierCheminETS2(string nouveauChemin)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\TrucksBook", writable: true);
            {
                if (key != null)
                {
                    key.SetValue("ets2", nouveauChemin, RegistryValueKind.String);
                    Console.WriteLine($"Chemin ETS2 modifié avec succès : {nouveauChemin}");
                }
                else
                {
                    throw new Exception("Clé de registre 'TrucksBook' introuvable.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur lors de la modification de la clé : " + ex.Message);
        }
    }

    #endregion



    #region Système pour récupérer le processus de TrucksBook
    static Process GetLogiciel(bool waitUntilFound = true)
    {
        string[] processNames = { "TB Client", "TrucksBook Client", "TB Client.exe" };

        Process? process = null;

        if (!waitUntilFound)
        {
            return GetProcessByNames(processNames);
        }


        while (process == null)
        {
            process = GetProcessByNames(processNames);

            Thread.Sleep(100);
            Console.WriteLine($"Tentative pour trouver le processus TrucksBook...");

        }
        return process;

    }


    static Process? GetProcessByNames(string[] names)
    {
        var lowerNames = names.Select(n => n.ToLowerInvariant()).ToHashSet();
        try
        {
            var process = Process.GetProcesses()
                .FirstOrDefault(p => lowerNames.Contains(p.ProcessName.ToLowerInvariant()));
#pragma warning disable CS8603 // Possible null reference return.
            return process;
#pragma warning restore CS8603 // Possible null reference return.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération des processus : {ex.Message}");
            return null;
        }
    }

    #endregion



    #region Système pour trouver le fichier eurotrucks2.exe

    public static string? FindEuroTrucks2Exe()
    {
        // Helper: verify that the immediate parent directory name contains "x64"
        static bool IsX64Parent(string path)
        {
            var parent = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
            return !string.IsNullOrEmpty(parent) && parent.IndexOf("x64", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 1) Try common Steam locations first (fast path)
        try
        {
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var steamRoot = Path.Combine(pf86, "Steam");

            var candidateDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddBinDirsUnder(string libraryPath)
            {
                try
                {
                    var bin = Path.Combine(libraryPath, "steamapps", "common", "Euro Truck Simulator 2", "bin");
                    if (Directory.Exists(bin))
                    {
                        foreach (var d in Directory.EnumerateDirectories(bin, "*x64*", SearchOption.TopDirectoryOnly))
                        {
                            candidateDirs.Add(d);
                        }
                    }
                }
                catch { /* ignore and continue */ }
            }

            if (Directory.Exists(steamRoot))
            {
                // Default Steam library
                AddBinDirsUnder(steamRoot);

                // Additional Steam libraries from libraryfolders.vdf
                try
                {
                    var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(vdf))
                    {
                        foreach (var line in File.ReadLines(vdf))
                        {
                            var m = Regex.Match(line, "\"path\"\\s+\"([^\"]+)\"");
                            if (m.Success)
                            {
                                var libPath = m.Groups[1].Value.Replace("\\\\", "\\");
                                if (Directory.Exists(libPath))
                                {
                                    AddBinDirsUnder(libPath);
                                }
                            }
                        }
                    }
                }
                catch { /* ignore parsing/IO errors */ }
            }

            foreach (var dir in candidateDirs)
            {
                var exe = Path.Combine(dir, "eurotrucks2.exe");
                if (File.Exists(exe) && IsX64Parent(exe))
                {
                    return exe;
                }
            }
        }
        catch { /* ignore and fallback to broad search */ }

        // 2) Fallback: scan all drives for directories containing "x64" and look for eurotrucks2.exe inside
        foreach (var drive in GetAllDrives())
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(drive, "*x64*", SearchOption.AllDirectories))
                {
                    var exe = Path.Combine(dir, "eurotrucks2.exe");
                    if (File.Exists(exe) && IsX64Parent(exe))
                    {
                        return exe;
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is DirectoryNotFoundException || ex is PathTooLongException)
            {
                // Ignore folders/drives we can't access
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la recherche de eurotrucks2.exe : {ex.Message}");
            }
        }

        return null;
    }


    public static List<string> GetAllDrives()
    {
        var drives = new List<string>();
        for (char drive = 'A'; drive <= 'Z'; drive++)
        {
            string driveLetter = $"{drive}:\\";
            try
            {
                if (Directory.Exists(driveLetter))
                {
                    drives.Add(driveLetter);
                }
            }
            catch { }
        }
        return drives;
    }

    #endregion


    #region Installation du mod

    public static void InstallMod()
    {
        Console.WriteLine("Installation du mod...");
        if (IsModUpToDate()) { return; }

        string modPath = GetModPath();
        if (modPath == null)
        {
            Console.WriteLine("ERREUR : Le dossier 'mod' n'a pas été trouvé.");
            return;
        }

        // Recherche du fichier zip du mod dans le dossier parent
        string modZipDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
        string[] modZips = Directory.GetFiles(modZipDirectory, "ModTGEv*.zip", SearchOption.TopDirectoryOnly);

        // Sélectionner le mod avec la version la plus haute
        string? dlModPath = null;
        Version? maxVersion = null;
        foreach (var zip in modZips)
        {
            string fileName = Path.GetFileNameWithoutExtension(zip);
            // Extraction de la version avec Regex
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"ModTGEv([0-9]+(?:\.[0-9]+)*)");
            if (match.Success)
            {
                if (Version.TryParse(match.Groups[1].Value, out Version? ver))
                {
                    if (maxVersion == null || ver > maxVersion)
                    {
                        maxVersion = ver;
                        dlModPath = zip;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(dlModPath) || !File.Exists(dlModPath))
        {
            Console.WriteLine($"ERREUR : Aucun fichier du mod 'ModTGEv*.zip' trouvé dans '{modZipDirectory}'.");
            return;
        }

        // Extraction du zip dans le dossier 'mod'
        try
        {
            // Déplacement du fichier zip dans le dossier mod
            string destZipPath = Path.Combine(modPath, Path.GetFileName(dlModPath));
            if (File.Exists(destZipPath))
            {
                File.Delete(destZipPath);
            }
            File.Move(dlModPath, destZipPath);
            Console.WriteLine($"Mod déplacé avec succès (version {maxVersion}) : {destZipPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'installation du mod : {ex.Message}");
        }
    }


    public static string? GetModPath()
    {
        // Chemin standard dans Documents
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string standardPath = Path.Combine(userProfile, "Documents", "Euro Truck Simulator 2", "mod");
        if (Directory.Exists(standardPath))
        {
            return standardPath;
        }

        // Recherche globale sur tous les disques
        foreach (var drive in GetAllDrives())
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(drive, "mod", SearchOption.AllDirectories))
                {
                    var parentDir = Path.GetFileName(Path.GetDirectoryName(dir));
                    var grandParentDir = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(dir)));
                    if (parentDir != null && parentDir.Equals("Euro Truck Simulator 2", StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
        }
        return null;
    }


    public static bool IsModUpToDate()
    {
        // TODO: Comparer la version du mod avec la version attendue
        // Logique pour vérifier si le mod est à jour
        return true;
    }


    #endregion





    public class Credentials
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public static void SauvegarderIdentifiants()
    {
        System.Windows.Forms.Form form = new System.Windows.Forms.Form
        {
            Text = "Connexion TrucksBook",
            Width = 320,
            Height = 230,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true // Toujours au premier plan
        };

        // Indique si la fermeture est volontaire (validation du formulaire)
        bool fermetureVolontaire = false;
        // Fermeture brutale du processus si la fenêtre est fermée sans enregistrement (croix)
        form.FormClosed += (s, e) =>
        {
            if (!fermetureVolontaire)
            {
                KillTrucksBook();
                Environment.Exit(0);
            }
        };

        // Titre principal
        System.Windows.Forms.Label lblTitle = new System.Windows.Forms.Label
        {
            Text = "Compte TrucksBook",
            Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
            Left = 10,
            Top = 10,
            Width = 280,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        };

        // Champ Email
        System.Windows.Forms.Label lblEmail = new System.Windows.Forms.Label { Left = 10, Top = 50, Text = "Email", Width = 80 };
        System.Windows.Forms.TextBox txtEmail = new System.Windows.Forms.TextBox { Left = 100, Top = 47, Width = 180 };
        txtEmail.Text = GetCredentials()?.Email ?? string.Empty; // Pré-remplissage si des identifiants existent

        // Champ Mot de passe
        System.Windows.Forms.Label lblPassword = new System.Windows.Forms.Label { Left = 10, Top = 85, Text = "Mot de passe", Width = 80 };
        System.Windows.Forms.TextBox txtPassword = new System.Windows.Forms.TextBox { Left = 100, Top = 82, Width = 180, PasswordChar = '*' };
        txtPassword.Text = GetCredentials()?.Password ?? string.Empty; // Pré-remplissage si des identifiants existent

        // Bouton Enregistrer
        System.Windows.Forms.Button btnSave = new System.Windows.Forms.Button { Text = "Enregistrer", Left = 100, Width = 180, Top = 120 };
        btnSave.Click += (sender, e) =>
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                System.Windows.Forms.MessageBox.Show("Veuillez remplir tous les champs.", "Erreur", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                System.Windows.Forms.MessageBox.Show("Adresse email invalide.", "Erreur", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            var creds = new Credentials
            {
                Email = email,
                Password = password
            };

            string json = JsonSerializer.Serialize(creds, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("credentials.json", json);

            fermetureVolontaire = true;
            form.Close(); // Ferme la fenêtre sans message
        };

        // Bouton Aide
        System.Windows.Forms.Button btnHelp = new System.Windows.Forms.Button { Text = "Aide", Left = 10, Top = 120, Width = 80 };
        btnHelp.Click += (sender, e) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/7yTzWATs66",
                UseShellExecute = true
            });
        };

        // Appui sur la touche "Entrée" pour valider
        form.KeyPreview = true;
        form.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                btnSave.PerformClick();
            }
        };

        // Ajout des contrôles à la fenêtre
        form.Controls.Add(lblTitle);
        form.Controls.Add(lblEmail);
        form.Controls.Add(txtEmail);
        form.Controls.Add(lblPassword);
        form.Controls.Add(txtPassword);
        form.Controls.Add(btnSave);
        form.Controls.Add(btnHelp);


        System.Windows.Forms.Application.Run(form);
    }
    public static Credentials? GetCredentials()
    {
        if (File.Exists("credentials.json"))
        {
            string json = File.ReadAllText("credentials.json");
            return JsonSerializer.Deserialize<Credentials>(json);
        }
        return null;
    }



    public static async Task<bool> IsLatestModVersionAsync()
    {
        long githubVersion = 0;
        long localVersion = 0;


        // 1. Trouver le fichier qui a pour regex tge_mod_([0-9]+)\.zip dans le dossier actuel
        var currentFolder = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory);
        Console.WriteLine($"Recherche des fichiers de mod dans : {AppDomain.CurrentDomain.BaseDirectory}");
        // Utiliser une regex pour extraire la version depuis le nom du fichier
        foreach (var file in currentFolder)
        {
            var match = Regex.Match(Path.GetFileName(file), @"tge_mod_([0-9]+)\.zip");
            if (match.Success && long.TryParse(match.Groups[1].Value, out var ver))
            {
                if (ver > localVersion)
                    localVersion = ver;
            }
        }

        if (localVersion == 0)
        {
            Console.WriteLine("Aucun fichier de mod local trouvé.");
            return false;
        }


        // 2. Récupérer la dernière version GitHub
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("tge-launcher-check");
            var url = "https://api.github.com/repos/Lenitra/tge-launcher/releases/latest";
            var response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name) && name.GetString()?.StartsWith("LauncherTGE_") == true)
                    {
                        var part = name.GetString()?.Split('_')[1].Split('.')[0];
                        githubVersion = long.TryParse(part, out var ver) ? ver : 0;
                    }
                }

            }
        }
        catch
        {
            Console.WriteLine("Erreur lors de la vérification de la version GitHub.");
            return true;
        }

        // 3. Comparer
        Console.WriteLine($"{localVersion} : Local");
        Console.WriteLine($"{githubVersion} : GitHub");
        Console.WriteLine($"Comparaison des versions du launcher : {(localVersion >= githubVersion ? "À jour" : "Obsolète")}");
        return localVersion >= githubVersion;

    }


    public static void CheckAndUpdateLauncher()
    {
        if (IsLatestModVersionAsync().Result)
        {
            Console.WriteLine("Le launcher est à jour.");
            return;
        }
        Console.WriteLine("Une nouvelle version du launcher est disponible.");
        DownloadLatestModVersion();
    }


    // TODO: 
    public static void RunInstallUpdate()
    {
        string batPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.bat");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{batPath}\"", // ← "start" ouvre une nouvelle fenêtre CMD indépendante
                UseShellExecute = false,
                CreateNoWindow = false
            }
        };

        process.Start();

        // Fermer l'application principale
        Environment.Exit(0);
    }

    public static void DownloadLatestModVersion()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("tge-launcher");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var url = "https://api.github.com/repos/Lenitra/tge-launcher/releases/latest";
            var response = client.GetStringAsync(url).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(response);
            string? downloadUrl = null;
            string? assetName = null;
            if (doc.RootElement.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name) && name.GetString()?.StartsWith("LauncherTGE_") == true &&
                        asset.TryGetProperty("browser_download_url", out var download))
                    {
                        downloadUrl = download.GetString();
                        assetName = name.GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                Console.WriteLine("Aucun asset 'LauncherTGE_*' trouvé sur la release GitHub.");
                return;
            }

            // Déterminer le nom de fichier de destination
            string fileName;
            try
            {
                var uri = new Uri(downloadUrl);
                fileName = Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                fileName = assetName ?? "LauncherTGE_latest.zip";
            }

            // Enregistrer dans un sous-dossier "build" proche de l'exécutable
            var buildDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build");
            Directory.CreateDirectory(buildDir);
            var destinationPath = Path.Combine(buildDir, fileName);

            Console.WriteLine($"Téléchargement de '{fileName}'...");
            using var resp = client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            resp.EnsureSuccessStatusCode();
            using var stream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.CopyTo(fs);
            fs.Flush(true);
            Console.WriteLine($"Fichier téléchargé: {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du téléchargement de la dernière version du mod : {ex.Message}");
        }
    }



    #region Utility Methods
    public static void ListInteractiveElementsWithFlaUI()
    {
        try
        {
            using var automation = new UIA3Automation();
            using var app = FlaUI.Core.Application.Attach(GetLogiciel());

            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return;
            }

            Console.WriteLine($"Fenêtre principale détectée : {window.Title}\n");
            Console.WriteLine("Exploration des éléments interactifs de la fenêtre :\n");

            ListElementsRecursive(window, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur lors de l'exploration de la fenêtre : " + ex.Message);
        }
    }

    private static void ListElementsRecursive(AutomationElement element, int level)
    {
        string indent = new string(' ', level * 2);
        string name = element.Name;
        string controlType = element.ControlType.ToString() ?? "?";
        string automationId = element.AutomationId;

        Console.WriteLine($"{indent}- [{controlType}] Name: '{name}'  Id: '{automationId}'");

        var children = element.FindAllChildren();
        foreach (var child in children)
        {
            ListElementsRecursive(child, level + 1);
        }
    }



    public static void DebugElement(AutomationElement element)
    {
        if (element == null)
        {
            Console.WriteLine("[DebugElement] Element est null.");
            return;
        }
        Console.WriteLine("[DebugElement] --- Détail de l'élément ---");
        Console.WriteLine($"Type: {element.ControlType}");
        Console.WriteLine($"Name: '{element.Name}'");
        Console.WriteLine($"AutomationId: '{element.AutomationId}'");
        Console.WriteLine($"ClassName: '{element.ClassName}'");
        Console.WriteLine($"FrameworkId: '{element.FrameworkType}'");
        Console.WriteLine($"BoundingRectangle: {element.BoundingRectangle}");
        Console.WriteLine($"IsEnabled: {element.IsEnabled}");
        Console.WriteLine($"IsOffscreen: {element.IsOffscreen}");
        Console.WriteLine("[DebugElement] --------------------------");
    }


    #endregion


}
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Dereference of a possibly null reference.
#pragma warning disable CS8600// Dereference of a possibly null reference.