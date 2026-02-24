# 📖 User Guide - EasySave v1.0

**EasySave** is a fast and efficient backup software application designed to automate and secure the transfer of your files while adapting to your hardware constraints.

---

## 🚀 1. Getting Started with EasySave
- **Launch**: Run `EasySave.exe` from your installation folder, or start the **Console/EasySave** project from your IDE.
- Upon opening, you will land directly on the **Main Dashboard**, which centralizes your backup jobs.

## 📁 2. Create and Manage Backups
The main tab allows you to configure your various backup *Jobs*:
- **New Job**: Click the creation button and provide the following information:
  - **Name**: An explicit title (e.g., *Accounting Backup*).
  - **Source**: The original folder containing the files to be backed up.
  - **Destination**: The directory where the files will be stored/copied.
  - **Type**: 
    - *Full*: Complete copy of all files from the source.
    - *Differential*: Copies only the files that have been modified or added since the last full backup.
- **Execution**: Use the control buttons to **Launch (▶)**, **Pause (⏸)**, or **Stop (⏹)** your backups at any time.
- **Progress**: A loading bar and file indicators keep you informed of the real-time progress.

## ⚙️ 3. Application Settings
Access the **Settings** tab to configure the software globally:
- **Language**: Change the interface language and apply the modifications instantly.
- **Log Format**: Choose the format for your backup logging: **JSON** or **XML**.
- **Business Software**: Specify a critical software (e.g., *Calculator*). If EasySave detects that this software is running, your backups will automatically be **paused** to leave 100% of the computer's power to it.
- **Priority Files & Size Limit**: Specify which extensions should be transferred first, or block files exceeding a certain size.

## 🔐 4. Protect Your Data (CryptoSoft)
EasySave incorporates an encryption module that secures your files against unauthorized access.
- **On-the-fly Encryption**: In the settings, indicate the list of extensions to protect (e.g., `.txt`, `.pdf`). EasySave will automatically encrypt these files during the backup process.
- **Manual Decryption (Decrypt Tab)**:
  1. Go to the **Decrypt** page.
  2. Select the folder containing your encrypted files.
  3. Enter your **secret key / password**.
  4. Click the action button to completely restore your files in plain text.

## 📄 5. Tracking: Logs and Real-time State
EasySave is completely transparent and audit-friendly:
- **State**: A dynamically updated file that tells you, down to the millisecond, how many files and bytes remain to be processed.
- **Daily Logs**: Records everything that has been backed up during the day, including processing time, source files, target files, and sizes.
- **Log Location**:
  1. Press `Windows Key + R`
  2. Type `%ProgramData%` and press Enter.
  3. Navigate to the `EasySave` folder, where you will find the `Logs` and `State` folders. 

---
_Cesi 2025-2026 FISA A3 - Project developed by Elio Faivre, Arthur Roux, and Evann Abrial._
