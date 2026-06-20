# 📍 Add Branch Button Location Guide

## Visual Layout

The **"➕ Add Branch"** button is located at the **BOTTOM-RIGHT** of the "Custom Branches" tab within the Repository Manager window.

```
┌─────────────────────────────────────────────────────────────────┐
│  REPOSITORY MANAGER                                    [_][□][×] │
├─────────────────────────────────────────────────────────────────┤
│  📁 Repositories  |  🌿 Custom Branches ← Click this tab        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                                                            │ │
│  │  [Custom Branch Items Listed Here]                        │ │
│  │  • Branch Name 1         [✏️] [🗑️]                        │ │
│  │  • Branch Name 2         [✏️] [🗑️]                        │ │
│  │  • Branch Name 3         [✏️] [🗑️]                        │ │
│  │                                                            │ │
│  │  [List continues scrolling if many branches]              │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│                         [➕ ADD BRANCH] ← HERE!               │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                            [CLOSE]              │
└─────────────────────────────────────────────────────────────────┘
```

---

## Step-by-Step: How to Click "Add Branch"

### 1️⃣ **Open Repository Manager**
```
Press: Ctrl+B
Or: Click the "Manage Repos" button in main window
```

### 2️⃣ **Navigate to Custom Branches Tab**
```
The window shows two tabs:
  📁 Repositories  |  🌿 Custom Branches
                         ↑
                  Click on this tab
```

### 3️⃣ **View the List**
```
The tab displays:
  ┌──────────────────────────────────────┐
  │ List of custom branches              │
  │ • main           [✏️] [🗑️]            │
  │ • develop        [✏️] [🗑️]            │
  │ • feature/auth   [✏️] [🗑️]            │
  └──────────────────────────────────────┘
```

### 4️⃣ **Click the Add Button**
```
At the BOTTOM RIGHT of the window:

                        [➕ ADD BRANCH]  ← Click here!
```

### 5️⃣ **Dialog Opens**
```
A dialog box appears:

    ┌─────────────────────────────────────┐
    │ 📋 Add/Edit Branch                  │
    ├─────────────────────────────────────┤
    │ Repository:                         │
    │ [Dropdown ▼]                        │
    │                                     │
    │ Branch Name:                        │
    │ [Text input field]                  │
    │                                     │
    │              [Save] [Cancel]        │
    └─────────────────────────────────────┘
```

---

## 📐 Button Details

| Property | Value |
|----------|-------|
| **Label** | ➕ ADD BRANCH |
| **Color** | Blue (#0874F7) |
| **Position** | Bottom-Right of "Custom Branches" tab |
| **Height** | 40 pixels |
| **Click Handler** | `AddBranch_Click()` |
| **Function** | Opens `AddBranchDialog` |

---

## 🎯 Layout Hierarchy

```
Window: RepositoryManager
  └─ Main Grid
      ├─ Row 0: TabControl
      │   ├─ Tab 1: Repositories
      │   │   ├─ Row 0: ListBox (repositories list)
      │   │   └─ Row 1: Buttons
      │   │       ├─ ➕ Add Repository
      │   │       └─ 🔄 Refresh Branches
      │   │
      │   └─ Tab 2: Custom Branches ← YOU ARE HERE
      │       ├─ Row 0: ListBox (branches list)
      │       └─ Row 1: Buttons ← BUTTON IS HERE
      │           └─ ➕ Add Branch ← THIS IS IT!
      │
      └─ Row 1: Footer
          └─ [Close] button
```

---

## 💡 If You Can't Find It

### Issue 1: Button Is Hidden Below Window Edge
**Solution:**
- Make the window taller by dragging the bottom edge down
- Or scroll within the Custom Branches list
- The button should become visible in Grid.Row="1"

### Issue 2: Dialog Is Blocking It
**Solution:**
- Close the "Add/Edit Branch" dialog first (click X or Cancel)
- Then the button becomes visible

### Issue 3: Tab Isn't Selected
**Solution:**
- Make sure you've clicked on the "🌿 Custom Branches" tab
- Don't stay on "📁 Repositories" tab
- The "Add Branch" button only appears in the Branches tab

### Issue 4: Window Is Too Small
**Solution:**
- Drag the window edges to make it larger
- Minimum recommended size: 700px wide × 600px tall
- The button needs space to display

---

## 🔧 Technical Details

### XAML Location
```xml
<!-- In RepositoryManager.xaml -->
<TabItem Header="🌿 Custom Branches">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>       <!-- Row 0: ListBox -->
            <RowDefinition Height="Auto"/>    <!-- Row 1: Buttons -->
        </Grid.RowDefinitions>

        <ListBox Grid.Row="0" ... />  <!-- Custom branches list -->

        <StackPanel Grid.Row="1" HorizontalAlignment="Right">
            <Button Content="➕ Add Branch"   ← HERE!
                    Click="AddBranch_Click"
                    ... />
        </StackPanel>
    </Grid>
</TabItem>
```

### Code Handler
```csharp
// In RepositoryManager.xaml.cs
private void AddBranch_Click(object sender, RoutedEventArgs e)
{
    var dialog = new AddBranchDialog(
        _repositories.Select(r => r.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList());

    if (dialog.ShowDialog() == true)
    {
        var newBranch = new AppSettings.BranchEntry
        {
            BranchName = dialog.BranchName,
            Repository = dialog.SelectedRepository
        };

        _settings.CustomBranches.Add(newBranch);
        _settings.Save();
        RefreshBranches();
    }
}
```

---

## ✅ Complete Workflow

```
1. Open Repository Manager
   └─ Press: Ctrl+B

2. Go to Custom Branches Tab
   └─ Click: 🌿 Custom Branches tab

3. Click Add Branch Button
   └─ Click: ➕ ADD BRANCH (bottom-right)

4. Fill in Dialog
   └─ Select Repository: [dropdown]
   └─ Enter Branch Name: [text field]

5. Save
   └─ Click: [Save] button

6. Branch Added
   └─ New branch appears in list
   └─ Can now use in merge operations
```

---

## 🎨 Visual Styling

The button is styled with:
- **Background Color**: Blue (#0874F7)
- **Text Color**: White
- **Icon**: ➕ (plus sign)
- **Font Size**: 14px
- **Font Weight**: Bold
- **Padding**: 15px horizontal, 10px vertical
- **Border**: None (BorderThickness="0")
- **Cursor**: Hand (pointer)
- **Height**: 40px

This makes it **easy to spot** as a primary action button.

---

## 📝 Summary

**Location**: Bottom-right of "Custom Branches" tab in Repository Manager window

**How to Open Repository Manager**: Press `Ctrl+B`

**Button Label**: ➕ ADD BRANCH

**Color**: Blue (#0874F7)

**Function**: Opens dialog to add a new custom branch

**Requirements**:
- At least one repository must exist
- Window must be large enough to display button

---

**Need Help?** Check the troubleshooting section above! ✅
