# BandProgram Avalonia Migration Plan

## Overview
Windows Forms (.NET Framework 4.5) -> Avalonia UI (.NET 8) 크로스플랫폼 마이그레이션

## Current Project Structure

### Original Files (BandProgram)
```
BandProgram/
├── Program.cs                 # Entry point
├── Login.cs                   # First login form (server auth)
├── LoginSecond.cs             # Band account selection
├── MainForm.cs                # Main window with tabs
├── NewPostForm.cs             # Add/edit post with comment
├── PostingAddForm.cs          # Add/edit comment/chat
├── SelectPostingForm.cs       # Select post numbers for bands
├── BandLayout.cs              # Tab layout controller
├── FunctionList.cs            # Core business logic (2300+ lines)
├── Util.cs                    # Utility class (Selenium, file I/O)
├── APIDAO.cs                  # API requests
├── Global.cs                  # Global settings
├── Band.cs                    # Band model
├── BandInfo.cs                # Band info model
├── ImageFile.cs               # Image file model
├── Post.cs                    # Post model
├── AccountInfo.cs             # Account model
├── Response.cs                # API response model
├── Naver.cs                   # Naver login
├── NaverMobile.cs             # Naver mobile
├── ADB.cs                     # Android Debug Bridge
└── IntCompare.cs              # ListView sort comparer
```

---

## Migration Tasks

### Phase 1: Project Setup [DONE]
- [x] Create .NET 8 Avalonia project
- [x] Configure NuGet packages (Avalonia, Selenium, Newtonsoft.Json)
- [x] Create folder structure (Models, Views, Services)

### Phase 2: Models [DONE]
- [x] Global.cs
- [x] Band.cs
- [x] BandInfo.cs
- [x] ImageFile.cs
- [x] Post.cs
- [ ] AccountInfo.cs
- [ ] Response.cs

### Phase 3: Services [PARTIAL]
- [x] Util.cs (basic migration)
- [ ] Util.cs - Complete all methods
- [ ] APIDAO.cs -> ApiService.cs
- [ ] FunctionList.cs -> BandService.cs (major refactor needed)
- [ ] Naver.cs -> NaverService.cs
- [ ] NaverMobile.cs -> merge into NaverService.cs
- [ ] ADB.cs -> AdbService.cs

### Phase 4: Views [PARTIAL]
- [x] LoginWindow.axaml (basic)
- [x] MainWindow.axaml (basic)
- [ ] LoginWindow - Complete functionality
- [ ] LoginSecondWindow.axaml - Band account selection
- [ ] MainWindow - Complete all tabs
- [ ] PostAddWindow.axaml - Add/edit posts
- [ ] CommentAddWindow.axaml - Add/edit comments
- [ ] SelectPostingWindow.axaml - Select post numbers

### Phase 5: ViewModels (MVVM Pattern)
- [ ] LoginViewModel.cs
- [ ] LoginSecondViewModel.cs
- [ ] MainViewModel.cs
- [ ] BandListViewModel.cs
- [ ] PostingViewModel.cs
- [ ] CommentViewModel.cs
- [ ] ChattingViewModel.cs

### Phase 6: Platform-Specific Changes
- [ ] WinHttp COM -> HttpClient (DONE in Util)
- [ ] Clipboard -> Avalonia.Input.Clipboard
- [ ] OpenFileDialog -> Avalonia Storage API
- [ ] FolderBrowserDialog -> Avalonia Storage API
- [ ] Application.StartupPath -> AppDomain.CurrentDomain.BaseDirectory
- [ ] MessageBox -> Avalonia MessageBox
- [ ] chromedriver.exe -> chromedriver (Linux binary)

---

## Detailed File Migration

### 1. LoginWindow (Login.cs)
**Original Features:**
- Server authentication (HTTP POST)
- Save/load credentials (acc.txt)
- Open LoginSecond on success

**Status:** Basic done, needs testing

### 2. LoginSecondWindow (LoginSecond.cs)
**Original Features:**
- List band accounts (bandAccount.txt)
- Add/remove accounts
- Base64 encode password
- Band login via Selenium
- Open MainWindow on success

**Migration Tasks:**
- [ ] Create LoginSecondWindow.axaml
- [ ] ListView -> DataGrid
- [ ] ComboBox for login type (Email/Phone/Naver)
- [ ] Context menu -> Avalonia ContextMenu

### 3. MainWindow (MainForm.cs + BandLayout.cs)
**Original Features:**
- 5 Tabs: Band List, Posting, Comment, Chat, (Band Join)
- Band list management (load, save, delete)
- Post/Comment/Chat work threads
- Log display
- Login session check thread

**Migration Tasks:**
- [ ] Complete all tab UIs
- [ ] Implement BandLayout as ViewModel
- [ ] Replace Thread with Task/async
- [ ] DataGrid for band list
- [ ] ListBox for logs

### 4. PostAddWindow (NewPostForm.cs)
**Original Features:**
- Post content text area
- Image list with add/remove
- Optional comment with images
- Folder/single file selection
- Save to AutoDoc/Posting/post_N/

**Migration Tasks:**
- [ ] Create PostAddWindow.axaml
- [ ] Avalonia file picker
- [ ] Image preview (optional)

### 5. CommentAddWindow (PostingAddForm.cs)
**Original Features:**
- Content text area
- Image list
- Save to AutoDoc/Comment/ or AutoDoc/Chatting/

**Migration Tasks:**
- [ ] Create CommentAddWindow.axaml
- [ ] Reuse PostAddWindow components

### 6. SelectPostingWindow (SelectPostingForm.cs)
**Original Features:**
- Input post/comment/chat numbers
- Apply to selected bands

**Migration Tasks:**
- [ ] Create SelectPostingWindow.axaml
- [ ] Simple dialog with TextBoxes

---

## FunctionList.cs Refactor (BandService.cs)

This is the largest file (~2300 lines). Split into:

### BandService.cs
- getBandList()
- getBandListFromQuery()
- getBandListInFile()
- removeBand()
- signupBand()

### PostingService.cs
- startPosting()
- getPostingList()
- getPostingContent()
- getPostingImages()
- savePosting()
- savePostingWithComment()

### CommentService.cs
- startComment()
- getCommentContent()
- getCommentImages()

### ChattingService.cs
- startChatting()
- getChattingContent()
- getChattingImages()

### Common Refactors
- Replace Thread.Suspend/Resume with CancellationToken
- Use async/await pattern
- Events -> IObservable or callbacks
- Delegate -> Action/Func

---

## Testing Plan

1. **Unit Tests**
   - Model serialization
   - Util methods
   - API service

2. **Integration Tests**
   - Selenium Chrome launch
   - File I/O operations

3. **Manual Tests**
   - Login flow
   - Band list load
   - Posting workflow

---

## Execution Order

```
Week 1: Phase 2-3 (Models + Services)
Week 2: Phase 4 (Views - basic UI)
Week 3: Phase 5 (ViewModels + binding)
Week 4: Phase 6 (Platform fixes) + Testing
```

---

## Files to Create

```
BandProgramAvalonia/
├── Program.cs                    [DONE]
├── App.axaml                     [DONE]
├── App.axaml.cs                  [DONE]
├── Models/
│   ├── Global.cs                 [DONE]
│   ├── Band.cs                   [DONE]
│   ├── BandInfo.cs               [DONE]
│   ├── ImageFile.cs              [DONE]
│   ├── Post.cs                   [DONE]
│   ├── AccountInfo.cs            [TODO]
│   └── Response.cs               [TODO]
├── Services/
│   ├── Util.cs                   [PARTIAL]
│   ├── ApiService.cs             [TODO]
│   ├── BandService.cs            [TODO]
│   ├── PostingService.cs         [TODO]
│   ├── CommentService.cs         [TODO]
│   ├── ChattingService.cs        [TODO]
│   ├── NaverService.cs           [TODO]
│   └── AdbService.cs             [TODO]
├── ViewModels/
│   ├── ViewModelBase.cs          [TODO]
│   ├── LoginViewModel.cs         [TODO]
│   ├── LoginSecondViewModel.cs   [TODO]
│   ├── MainViewModel.cs          [TODO]
│   └── ...                       [TODO]
└── Views/
    ├── LoginWindow.axaml         [DONE]
    ├── LoginWindow.axaml.cs      [DONE]
    ├── LoginSecondWindow.axaml   [TODO]
    ├── MainWindow.axaml          [PARTIAL]
    ├── MainWindow.axaml.cs       [PARTIAL]
    ├── PostAddWindow.axaml       [TODO]
    ├── CommentAddWindow.axaml    [TODO]
    └── SelectPostingWindow.axaml [TODO]
```
