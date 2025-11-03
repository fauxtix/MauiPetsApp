# New Features – MauiPets

This document summarizes the main features recently added (Oct/2025).

---
<img width="388" height="800" alt="Update_10_2025" src="https://github.com/user-attachments/assets/af31e042-ea8f-4f18-9416-01e1e79e3b6f" />
---

## 📸 Pet Photo Gallery

- **Per-Pet Photo Gallery**
  - Each pet now has an associated photo gallery.
  - Features include:
    - Add photos (using device camera or gallery).
    - View photos in a gallery mode.
    - Delete individual photos.
    - Enlarge/view a photo in a popup.
  - Photos are stored locally in the app and linked to the respective pet.

- **UI/UX Integration**
  - Access the gallery directly from the pet profile.
  - Visual confirmation and toast messages for user actions (e.g., photo deletion).
  - Easy navigation between the gallery and pet details.

---

# 📄 Documents Management - MauiPetsApp

The Documents Management functionality enables users to upload, view, edit, and delete files—such as vaccination records, certificates, or any relevant pet-related documents—linked to individual pets.

## ✨ Features

- ➕ **Add Documents:**  
  Upload PDF files using a file picker, add title and description, and associate each document with a specific pet.

- 👀 **View Documents:**  
  See all documents belonging to a pet, including title, description, file location, creation date, and associated pet name.

- ✏️ **Edit Documents:**  
  Change a document's title/description or replace the uploaded file.

- 🗑️ **Delete Documents:**  
  Remove documents from both the database and local storage; includes confirmation dialogs.

- 📂 **Open Documents:**  
  Launch files using the associated file path with the system's file viewer.

## 🛠 Technical Details

- 🧩 **MVVM Pattern:**  
  The main ViewModel (`PetDocumentsViewModel`) manages document records per pet, handles picking/storage, editing, and deletion.
  
- 🔗 **Service and Repository Layer:**  
  - The service layer (`DocumentsService`) converts between models, applies business rules, and calls the repository.
  - The repository (`DocumentsRepository`) interfaces with the database for CRUD operations on the `Documento` table.

- 🗃️ **Data Model**
  - `Documento`/`DocumentoDto`/`DocumentoVM`: Store ID, Title, Description, DocumentPath, CreatedOn, PetId, and (for view model) PetName.

## 🚦 Usage Workflow

1. **Add a Document:**  
   - From a pet's profile, select "Add Document" ➕.
   - Pick a PDF file 📄.
   - Enter a Title and Description 📝.
   - Save to link the document with the pet 🐾.

2. **Edit or Remove a Document:**  
   - Select a document entry from the list 📃.
   - Edit its details ✏️ or click delete 🗑️ for confirmation and removal.

3. **Open/View Document:**  
   - Tap a document to open it with the system PDF viewer 📂.

## 💡 Notes

- Only PDF documents are supported for upload.
- Files are saved locally within the app's data directory.
- The system ensures documents are uniquely named to avoid conflicts.
- Deleting a document also cleans up associated local storage if the file exists.

---

For implementation details or developer documentation, see:

- [PetDocumentsViewModel.cs](https://github.com/fauxtix/MauiPetsApp/blob/main/MauiPets/Mvvm/ViewModels/Documents/PetDocumentsViewModel.cs)
- [DocumentsService.cs](https://github.com/fauxtix/MauiPetsApp/blob/main/MauiPetsApp.Infrastructure/Services/DocumentsService.cs)
- [DocumentsRepository.cs](https://github.com/fauxtix/MauiPetsApp/blob/main/MauiPetsApp.Infrastructure/Repositories/DocumentsRepository.cs)

---
### 📢 Notifications

**Purpose:**  
This feature aims to alert users to the existence of notifications within the application that have not yet been marked as read or processed.  
It is suitable for system messages, event reminders, pending tasks, or any alert requiring user attention.

**What appears on the page:**  
- A bell icon is displayed at the top right of the main page.
- When there are notifications not yet marked as read, a red badge appears over the bell, showing the number of pending notifications.
- By tapping the bell, the user accesses the list of notifications.
- Each notification can be individually marked as read/processed by the user, removing it from the badge count.
- Only notifications not yet marked as read (unprocessed) are counted and shown; read notifications are not considered.

**Types of notifications supported:**  
- Event or appointment reminders.
- Alerts for pending tasks.
- System warning messages.
- Other internal communications requiring user action.

---

## 🔐 Data Backup and Restore

- **Manual Backup**
  - Ability to create backups of the app's local database via the interface.
  - Users can see the name, date, and location of the last backup.
  - Backup is saved as a local file, with visual indication of success/error.
  - Protection against accidental overwriting: confirmation before replacing existing backups.

- **Secure Restore**
  - Restore the local database from an existing backup.
  - Mandatory confirmation before replacing current data.
  - Visual information about differences between the current state and the backup.
  - Restore process with user feedback and clear success/failure messages.

---

## 📄 Pet Profile PDF Export & Sharing

- **Generate Detailed Pet Profile PDF**
  - Create a comprehensive PDF file for any pet, including:
    - Main data (name, species, breed, age, chip, etc.)
    - Vaccination, deworming, food, and vet consultation history.
  
- **Easy Sharing**
  - The PDF can be shared directly through the device’s native share options (email, WhatsApp, etc.).  

---

## Security and Privacy

- **Validation and Confirmation for Critical Actions**
  - Backup/restore and photo deletion actions require user confirmation.
  - Clear messages and visual feedback for all sensitive operations.

- **Local Data Management**
  - Photos and backup files are managed locally, respecting user privacy.
  - No sensitive data is sent to external servers without user action.

---

*For more details on each feature, explore the app interface.*
