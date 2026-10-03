# OpenCode GitHub 集成 - 配置完成

## ✅ 已完成的安装和配置

### 已安装的工具
- **Git**: v2.47.1 ✓
- **Node.js**: v20.18.0 LTS ✓
- **npm**: v10.8.2 ✓
- **OpenCode CLI**: v2.0.22 ✓

### 已创建的配置文件
1. `.github/workflows/opencode.yml` - GitHub Actions 工作流
2. `.gitignore` - Git 忽略文件
3. `.opencode.example.json` - OpenCode 配置示例
4. `GITHUB_SETUP.md` - 详细配置指南

### Git 仓库状态
- 仓库已初始化 ✓
- 所有文件已提交 ✓
- 工作目录干净 ✓

---

## 🚀 下一步：连接到 GitHub

### 1. 创建 GitHub 仓库
访问 https://github.com/new
- Repository name: `ChineseChess`
- 选择 Public 或 Private
- **不要**勾选 "Add a README file"

### 2. 推送代码到 GitHub
在项目目录打开命令行，运行：

```bash
git remote add origin https://github.com/YOUR_USERNAME/ChineseChess.git
git branch -M main
git push -u origin main
```

（将 `YOUR_USERNAME` 替换为你的 GitHub 用户名）

### 3. 安装 OpenCode GitHub App
访问 https://github.com/apps/opencode-agent
- 点击 "Install"
- 选择你的仓库

### 4. 配置 API 密钥
在仓库 Settings → Secrets and variables → Actions 中添加：

| Secret 名称 | 值 |
|-------------|-----|
| `ANTHROPIC_API_KEY` | 你的 Anthropic API 密钥 |

获取 API 密钥：https://console.anthropic.com/

---

## 📖 使用方法

在 GitHub Issue 或 PR 中使用以下命令：

### 解释问题
```
/opencode explain this issue
```

### 修复问题
```
/opencode fix this
```

### 审查代码
```
Review this PR for code quality /oc
```

### 特定代码行评论
在 PR 的 "Files" 选项卡中对代码行评论：
```
/oc add error handling here
```

---

## 🔧 工作流说明

已创建的工作流文件 `.github/workflows/opencode.yml` 支持：

| 事件 | 触发方式 |
|------|---------|
| `issue_comment` | 在 Issue 或 PR 中评论 |
| `pull_request_review_comment` | 在 PR 代码行评论 |

当评论中包含 `/opencode` 或 `/oc` 时，OpenCode 会自动执行任务。

---

## 📚 更多资源

- OpenCode 官网: https://opencode.ai
- GitHub 集成文档: https://opencode.ai/docs/github/
- Anthropic API: https://console.anthropic.com/

---

## ⚠️ 注意事项

1. **API 密钥安全**：永远不要将 API 密钥提交到代码仓库
2. **费用**：使用 Anthropic API 会产生费用，请查看定价
3. **权限**：确保 GitHub App 有足够的权限操作仓库

---

配置完成时间：2026-10-03