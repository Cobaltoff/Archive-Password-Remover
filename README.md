# 🔓 Archive Password Remover

A lightweight, fast, and user-friendly Windows desktop application designed to seamlessly remove known passwords from encrypted ZIP and RAR archives. 

**Note:** This is *not* a password cracking or brute-force tool. It is a legitimate utility for decrypting and repacking archives when you already know the password, saving you from entering it repeatedly in the future.

## ✨ Features
* **Drag & Drop Interface:** Modern, clean UI built with Windows Forms.
* **Multi-Format Support:** Handles both `.zip` and `.rar` extensions using the SharpCompress library.
* **Asynchronous Processing:** Keeps the UI responsive during heavy I/O operations without freezing.
* **Smart Memory Management:** Unpacks files to memory and repacks them directly into a new, unencrypted archive.
* **Self-Contained Executable:** Compiled as a single `.exe` file containing all necessary .NET libraries and native Windows dependencies. No installation required.

## 🛠️ Tech Stack
* **Language:** C#
* **Framework:** .NET (Windows Forms)
* **Core Library:** [SharpCompress](https://github.com/adamhathcock/sharpcompress) (for archive extraction and manipulation)
* **Architecture:** Asynchronous Task programming (`async/await`)

## 🚀 How to Use
1. Download the latest `SifreKaldir.exe` from the [Releases](#) section.
2. Drag and drop your locked `.zip` or `.rar` file into the application.
3. Enter the correct password for the archive.
4. Select a save location (the app will remember this for future uses).
5. Click **Start**. The app will generate a brand new, password-free version of your archive.

## 💡 Why I Built This
This project was developed to deepen my understanding of stream manipulation, asynchronous programming in C#, and utilizing third-party NuGet packages within a clean, user-centric interface.
