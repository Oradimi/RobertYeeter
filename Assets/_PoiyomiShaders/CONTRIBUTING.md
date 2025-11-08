# Contributing to Poiyomi Shaders

## Git Configuration for Line Endings

To prevent line ending issues when contributing to this project, please configure your git settings properly:

### For Windows Users
Run this command to ensure proper line ending handling:
```bash
git config core.autocrlf true
```

This ensures that:
- Files are checked out with Windows line endings (CRLF) for local editing
- Files are committed with Unix line endings (LF) to maintain repository consistency

### For Mac/Linux Users
Run this command:
```bash
git config core.autocrlf input
```

This ensures that:
- Files keep Unix line endings (LF) locally
- Any accidental CRLF line endings are converted to LF on commit

### Why This Matters
- Prevents unnecessary diffs caused by line ending changes
- Ensures consistency across different operating systems
- Keeps the repository clean and merge conflicts minimal

The `.gitattributes` file in this repository enforces LF line endings for all text files in the repository, regardless of your local system.
