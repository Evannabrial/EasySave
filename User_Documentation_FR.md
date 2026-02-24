# 📖 Guide Utilisateur - EasySave v1.0

**EasySave** est une application logicielle de sauvegarde rapide et efficace, conçue pour automatiser et sécuriser le transfert de vos fichiers tout en s'adaptant à vos contraintes matérielles.

---

## 🚀 1. Démarrer avec EasySave
- **Lancement** : Exécutez `EasySave.exe` depuis votre dossier d'installation, ou lancez le projet **Console/EasySave** depuis votre IDE.
- À l'ouverture, vous arrivez directement sur le **Tableau de bord principal** centralisant vos travaux de sauvegarde.

## 📁 2. Créer et Gérer des Sauvegardes
L'onglet principal vous permet de configurer vos différents *Jobs* de sauvegarde :
- **Nouveau Job** : Cliquez sur le bouton de création et renseignez les informations suivantes :
  - **Name (Nom)** : Un titre explicite (ex: *Sauvegarde Compta*).
  - **Source** : Le dossier d'origine contenant les fichiers à sauvegarder.
  - **Destination** : Le répertoire où les fichiers seront stockés/copiés.
  - **Type** : 
    - *Full (Complète)* : Copie intégrale de tous les fichiers de la source.
    - *Differential (Différentielle)* : Ne copie que les fichiers modifiés et ajoutés depuis la dernière sauvegarde complète.
- **Exécution** : Utilisez les boutons de contrôle pour **Lancer (▶)**, **Mettre en pause (⏸)** ou **Arrêter (⏹)** vos sauvegardes à tout moment.
- **Progression** : Une barre de chargement et des indicateurs de fichiers vous informent de l'avancement en temps réel.

## ⚙️ 3. Paramètres de l'Application
Accédez à l'onglet **Settings (Paramètres)** pour configurer globalement le logiciel :
- **Language (Langue)** : Changez la langue de l'interface et appliquez les modifications instantanément.
- **Log Format** : Choisissez le format de votre journalisation de sauvegardes : **JSON** ou **XML**.
- **Business Software (Logiciel Métier)** : Renseignez un logiciel critique (ex: *Calculator*). Si EasySave détecte que ce dernier est en cours d'exécution, vos sauvegardes seront automatiquement mises en **pause** pour lui laisser 100% de la puissance de l'ordinateur.
- **Fichiers Prioritaires & Taille Limite** : Renseignez quelles extensions doivent être transférées en premier, ou bloquez les fichiers dépassant un certain poids.

## 🔐 4. Protéger vos Données (CryptoSoft)
EasySave intègre un module de chiffrement qui sécurise vos fichiers contre les lectures non autorisées.
- **Chiffrement à la volée** : Dans les paramètres, indiquez la liste des extensions à protéger (ex: `.txt`, `.pdf`). EasySave chiffrera automatiquement ces fichiers lors de la sauvegarde.
- **Déchiffrement manuel (Onglet Decrypt)** :
  1. Rendez-vous dans la page **Decrypt**.
  2. Sélectionnez le dossier contenant vos fichiers chiffrés.
  3. Saisissez votre **clé secrète / mot de passe**.
  4. Cliquez sur le bouton d'action pour restaurer l'intégralité de vos fichiers en clair.

## 📄 5. Suivi : Logs et État en temps réel
EasySave est totalement transparent et audit-friendly :
- **État (State)** : Fichier mis à jour dynamiquement pour vous dire, à la milliseconde près, combien de fichiers et d'octets restent à traiter.
- **Historique Quotidien (Logs)** : Mémorise tout ce qui a été sauvegardé au cours de la journée avec la durée de traitement (fichiers sources, cibles, tailles).
- **Emplacement des journaux** :
  1. Appuyez sur `Touche Windows + R`
  2. Tapez `%ProgramData%` et appuyez sur Entrée.
  3. Naviguez vers le dossier `EasySave`, et vous y trouverez les dossiers `Logs` et `State`. 

## 🐳 6. Centralisation des Logs (Docker)
Pour les déploiements avancés, vous pouvez utiliser notre serveur de logs centralisé via Docker.

**Création de l'image (Build)** :
```bash
docker build -t easysave-logserver .
```

**Lancement du conteneur (Run)** :
```bash
docker run -d -p 4242:4242 -v volume_logs:/app/logs --name EasySaveLogServer easysave-logserver
```

---
_Cesi 2025-2026 FISA A3 - Projet développé par Elio Faivre, Arthur Roux, et Evann Abrial._
