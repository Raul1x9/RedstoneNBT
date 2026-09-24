# Contributing to Redstone NBT

Thank you for your interest in contributing to **Redstone NBT**! Whether you are fixing a bug, improving documentation, refining UI ergonomics, or adding support for new Minecraft formats, your help is warmly welcomed.

---

## 🧭 How to Contribute

### 1. Reporting Bugs
- Before submitting a bug report, check the [existing Issues](https://github.com/Raul1x9/RedstoneNBT/issues) to make sure it hasn't already been reported.
- If it's a new issue, please use our **[Bug Report Template](https://github.com/Raul1x9/RedstoneNBT/issues/new?template=bug_report.md)**.
- Include your operating system, Minecraft version/format (Java 1.21+, Bedrock, etc.), clear reproduction steps, and a sample `.dat` / `.mca` / `.nbt` file if possible.

### 2. Suggesting Features
- For new features or significant changes, please **open an issue first** to discuss your idea with the maintainer before investing substantial time writing code.
- Use our **[Feature Request Template](https://github.com/Raul1x9/RedstoneNBT/issues/new?template=feature_request.md)** to describe the problem and proposed solution.

### 3. Pull Requests (PRs)
- **Bug fixes, documentation improvements, and typo corrections**: Direct Pull Requests are always welcome! You do not need to open an issue first for small fixes.
- **New features and large refactors**: Please link your PR to an existing, discussed issue.

---

## 🛠️ Local Development Setup

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Git

### Building the Project
```bash
# 1. Clone your fork
git clone https://github.com/<your-username>/RedstoneNBT.git
cd RedstoneNBT

# 2. Build the solution
dotnet build RedstoneNBT.sln

# 3. Launch Redstone NBT
dotnet run --project src/RedstoneNBT/RedstoneNBT.csproj
```

---

## 📋 Pull Request Guidelines

1. **Branching**: Create a feature branch from the latest `main` branch:
   ```bash
   git checkout -b fix/issue-description
   # or
   git checkout -b feat/feature-name
   ```
2. **Build Cleanliness**: Ensure the entire solution builds with **0 errors and 0 warnings**:
   ```bash
   dotnet build RedstoneNBT.sln
   ```
3. **Commit Messages**: Follow [Conventional Commits](https://www.conventionalcommits.org/) format:
   - `fix: fix coordinate calculation in Anvil chunk header`
   - `feat: add SNBT format import/export`
   - `docs: update Linux packaging instructions in README`
   - `refactor: clean up TagKey natural comparer`
4. **Code Style**: Match existing codebase conventions:
   - MVVM architecture with Avalonia UI and CommunityToolkit.Mvvm.
   - Clean, readable C# with descriptive variable and method names.

---

## ⚖️ Licensing of Contributions

By contributing to **Redstone NBT**, you agree that your contributions will be licensed under the project's **[GNU General Public License v3.0 (GPLv3)](LICENSE)**.
