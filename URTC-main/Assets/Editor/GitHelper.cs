using LibGit2Sharp;
using System;
using UnityEngine;

namespace URTC.Editor
{
    /// <summary>
    /// GitHelper - Professional Git operations manager using LibGit2Sharp
    /// Provides comprehensive version control functionality for Unity projects
    /// </summary>
    public class GitHelper
    {
        #region Properties
        
        /// <summary>
        /// Path to the repository
        /// </summary>
        public string RepositoryPath { get; private set; }
        
        /// <summary>
        /// Author information for commits
        /// </summary>
        public Signature Author { get; private set; }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initialize GitHelper with author information
        /// </summary>
        /// <param name="authorName">Name of the commit author</param>
        /// <param name="authorEmail">Email of the commit author</param>
        public GitHelper(string authorName, string authorEmail)
        {
            Author = new Signature(authorName, authorEmail, DateTime.Now);
        }
        
        #endregion
        
        #region Core Git Operations
        
        /// <summary>
        /// 1. Initialize a new Git repository
        /// Equivalent to: git init
        /// </summary>
        /// <param name="path">Path where repository should be initialized</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool InitializeRepository(string path)
        {
            try
            {
                RepositoryPath = Repository.Init(path);
                Debug.Log($"[GitHelper] Repository initialized at: {RepositoryPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHelper] Failed to initialize repository: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 2. Stage all files for commit
        /// Equivalent to: git add .
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool StageAllFiles()
        {
            try
            {
                using var repo = new Repository(RepositoryPath);
                Commands.Stage(repo, "*");
                Debug.Log("[GitHelper] All files staged successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHelper] Failed to stage files: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 3. Commit staged changes
        /// Equivalent to: git commit -m "message"
        /// </summary>
        /// <param name="message">Commit message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool CommitChanges(string message)
        {
            try
            {
                using var repo = new Repository(RepositoryPath);
                var commit = repo.Commit(message, Author, Author);
                Debug.Log($"[GitHelper] Commit successful: {commit.Sha.Substring(0, 7)} - {message}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHelper] Failed to commit: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 4. Create or switch to 'main' branch
        /// Equivalent to: git branch -M main
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool CreateOrSwitchToMainBranch()
        {
            try
            {
                using var repo = new Repository(RepositoryPath);
                
                // Get or create 'main' branch
                var mainBranch = repo.Branches["main"] ?? repo.CreateBranch("main", repo.Head.Tip);
                
                // Checkout main branch
                Commands.Checkout(repo, mainBranch);
                
                Debug.Log("[GitHelper] Switched to 'main' branch successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHelper] Failed to create/switch to main branch: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 5. Add a remote repository
        /// Equivalent to: git remote add origin <url>
        /// </summary>
        /// <param name="remoteName">Name of the remote (typically 'origin')</param>
        /// <param name="remoteUrl">URL of the remote repository</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool AddRemote(string remoteName, string remoteUrl)
        {
            try
            {
                using var repo = new Repository(RepositoryPath);
                
                // Check if remote already exists
                var existingRemote = repo.Network.Remotes[remoteName];
                if (existingRemote != null)
                {
                    Debug.LogWarning($"[GitHelper] Remote '{remoteName}' already exists: {existingRemote.Url}");
                    return true;
                }
                
                // Add new remote
                repo.Network.Remotes.Add(remoteName, remoteUrl);
                Debug.Log($"[GitHelper] Remote '{remoteName}' added: {remoteUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHelper] Failed to add remote: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 6. Push branch to remote with tracking
        /// Equivalent to: git push -u origin main
        /// </summary>
        /// <param name="remoteName">Name of the remote (typically 'origin')</param>
        /// <param name="branchName">Name of the branch to push (typically 'main')</param>
        /// <param name="username">Git username or token</param>
        /// <param name="password">Git password or personal access token</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool PushToRemote(string remoteName, string branchName, string username, string password)
        {
            try
            {
                using var repo = new Repository(RepositoryPath);
                
                // Get the branch
                var branch = repo.Branches[branchName];
                if (branch == null)
                {
                    Debug.LogError($"[GitHelper] Branch '{branchName}' not found");
                    return false;
                }
                
                // Get the remote
                var remote = repo.Network.Remotes[remoteName];
                if (remote == null)
                {
                    Debug.LogError($"[GitHelper] Remote '{remoteName}' not found");
                    return false;
                }
                
                // Setup push options with credentials
                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (url, user, cred) =>
                        new UsernamePasswordCredentials
                        {
                            Username = username,
                            Password = password
                        }
                };
                
                // Push the branch
                repo.Network.Push(branch, pushOptions);
                
                // Set upstream tracking
                repo.Branches.Update(branch, b =>
                {
                    b.Remote = remote.Name;
                    b.UpstreamBranch = branch.CanonicalName;
                });
                
                Debug.Log($"[GitHelper] Successfully pushed '{branchName}' to '{remoteName}' with upstream tracking");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHelper] Failed to push: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 7. Pull changes from remote
        /// Equivalent to: git pull origin main
        /// </summary>
        /// <param name="remoteName">Name of the remote (typically 'origin')</param>
        /// <param name="branchName">Name of the branch (typically 'main')</param>
        /// <param name="username">Git username or token</param>
        /// <param name="password">Git password or personal access token</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool PullFromRemote(string remoteName, string branchName, string username, string password)
        {
            try
            {
                using var repo = new Repository(RepositoryPath);
                
                // Setup pull options with credentials
                var pullOptions = new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = (url, user, cred) =>
                            new UsernamePasswordCredentials
                            {
                                Username = username,
                                Password = password
                            }
                    }
                };

                // Pull the changes
                var signature = new Signature(Author.Name, Author.Email, DateTime.Now);
                Commands.Pull(repo, signature, pullOptions);
                
                Debug.Log($"[GitHelper] Successfully pulled from '{remoteName}'");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHelper] Failed to pull: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region High-Level Workflow Methods
        
        /// <summary>
        /// Complete Git workflow: Init → Add → Commit → Branch → Remote → Push
        /// This executes all Git operations in sequence
        /// </summary>
        /// <param name="localPath">Local repository path</param>
        /// <param name="commitMessage">First commit message</param>
        /// <param name="remoteUrl">Remote repository URL</param>
        /// <param name="username">Git username or token</param>
        /// <param name="password">Git password or personal access token</param>
        /// <returns>True if all operations successful, false otherwise</returns>
        public bool ExecuteFullGitWorkflow(
            string localPath,
            string commitMessage,
            string remoteUrl,
            string username,
            string password)
        {
            Debug.Log("[GitHelper] ===== Starting Full Git Workflow =====");
            
            // 1. Initialize repository
            if (!InitializeRepository(localPath))
                return false;
            
            // 2. Stage all files
            if (!StageAllFiles())
                return false;
            
            // 3. Commit changes
            if (!CommitChanges(commitMessage))
                return false;
            
            // 4. Create and switch to main branch
            if (!CreateOrSwitchToMainBranch())
                return false;
            
            // 5. Add remote origin
            if (!AddRemote("origin", remoteUrl))
                return false;
            
            // 6. Push to remote with tracking
            if (!PushToRemote("origin", "main", username, password))
                return false;
            
            Debug.Log("[GitHelper] ===== Full Git Workflow Completed Successfully =====");
            return true;
        }
        
        /// <summary>
        /// Quick commit and push workflow for existing repositories
        /// </summary>
        /// <param name="commitMessage">Commit message</param>
        /// <param name="username">Git username or token</param>
        /// <param name="password">Git password or personal access token</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool QuickCommitAndPush(string commitMessage, string username, string password)
        {
            Debug.Log("[GitHelper] ===== Quick Commit & Push =====");
            
            if (!StageAllFiles())
                return false;
            
            if (!CommitChanges(commitMessage))
                return false;
            
            if (!PushToRemote("origin", "main", username, password))
                return false;
            
            Debug.Log("[GitHelper] ===== Commit & Push Completed =====");
            return true;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get current repository status
        /// </summary>
        /// <returns>Repository status information</returns>
        public string GetRepositoryStatus()
        {
            try
            {
                using var repo = new Repository(RepositoryPath);
                var status = repo.RetrieveStatus();
                
                return $"Modified: {status.Modified.Count()}, " +
                       $"Added: {status.Added.Count()}, " +
                       $"Removed: {status.Removed.Count()}, " +
                       $"Untracked: {status.Untracked.Count()}";
            }
            catch (Exception ex)
            {
                return $"Error retrieving status: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Check if path is a valid Git repository
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if valid repository, false otherwise</returns>
        public static bool IsValidRepository(string path)
        {
            return Repository.IsValid(path);
        }
        
        #endregion
    }
}

