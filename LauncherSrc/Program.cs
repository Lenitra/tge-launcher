using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Win32;

public class Program
{
    static void Main(string[] args)
    {
        // Initialiser WinForms rendering settings AVANT toute création de fenêtre WinForms
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        bool etsStarted = false;

        Console.WriteLine("Essai de kill du processus TrucksBook...");
        KillTrucksBook();
        Thread.Sleep(1000); // Attendre un peu pour s'assurer que le processus est bien terminé

        Console.WriteLine("Installation du mod et configuration automatique du chemin ETS2...");
        InstallMod();
        ModifierCheminETS2(FindEuroTrucks2Exe() ?? "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Euro Truck Simulator 2\\bin\\win_x64\\eurotrucks2.exe");

        Console.WriteLine("Démarrage de TrucksBook...");
        StartTrucksBook();

        int counter = 0;
        while (!etsStarted)
        {
            counter++;
            if (DetectPageOnTrucksBook() == "login")
            {

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (counter > 1 || GetCredentials() == null || GetCredentials().Email == string.Empty || GetCredentials().Password == string.Empty)
                {
                    SauvegarderIdentifiants();
                    counter = 0; // Réinitialiser le compteur après la connexion
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                // Connexion à TrucksBook
                LoginToTrucksBook();
            }

            Console.WriteLine("En attente de la page d'accueil de TrucksBook...");

            if (DetectPageOnTrucksBook() == "home")
            {

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


    public static void ModifierCheminETS2(string nouveauChemin)
    {
        try
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\TrucksBook", writable: true))
            {
                if (key != null)
                {
                    key.SetValue("ets2", nouveauChemin, RegistryValueKind.String);
                }
                else
                {
                    throw new Exception("Clé de registre 'TrucksBook' introuvable.");
                }
            }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
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
#pragma warning disable CS8603 // Possible null reference return.
            return GetProcessByNames(processNames);
#pragma warning restore CS8603 // Possible null reference return.
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



    #region Système pour trouver le fichier eurotrucks2.exe
    public static string? FindEuroTrucks2Exe()
    {
        foreach (var drive in GetAllDrives())
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(drive, "eurotrucks2.exe", SearchOption.AllDirectories))
                {
                    var parentDir = Path.GetFileName(Path.GetDirectoryName(file));
                    if (parentDir != null && parentDir.Equals("x64", StringComparison.OrdinalIgnoreCase))
                    {
                        return file;
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is DirectoryNotFoundException)
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

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        string modPath = GetModPath();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
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
        // 1. Trouver la version locale
        string buildDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "build");
        if (!Directory.Exists(buildDir))
            return false;

        string[] modZips = Directory.GetFiles(buildDir, "tge_mod_*.zip", SearchOption.AllDirectories);
        if (modZips.Length == 0)
            return false;

        string? localVersion = null;
        foreach (var zip in modZips)
        {
            var match = Regex.Match(Path.GetFileName(zip), @"tge_mod_(\\d+)\\.zip");
            if (match.Success)
            {
                if (localVersion == null || String.Compare(match.Groups[1].Value, localVersion) > 0)
                    localVersion = match.Groups[1].Value;
            }
        }
        if (localVersion == null)
            return false;

        // 2. Récupérer la dernière version GitHub
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("tge-launcher-check");
            var url = "https://api.github.com/repos/Lenitra/tge-launcher/releases/latest";
            var response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            string? githubVersion = null;
            if (doc.RootElement.TryGetProperty("tag_name", out var tag))
                githubVersion = tag.GetString();
            if (string.IsNullOrWhiteSpace(githubVersion))
                return false;

            githubVersion = githubVersion.TrimStart('v', 'V');

            // 3. Comparer
            return localVersion == githubVersion;
        }
        catch
        {
            return false;
        }
    }


}

