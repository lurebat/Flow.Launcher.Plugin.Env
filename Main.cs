using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.Env
{
    public class Env : IPlugin
    {
        private PluginInitContext _context;

        private const string IconPath = "icon.png";
        private const string ClipboardErrorMsg = "Failed to copy to clipboard.";
        private const string SetVarErrorMsg = "Failed to set variable: ";
        private const string DeleteVarErrorMsg = "Failed to delete variable: ";

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            var first = query.FirstSearch?.ToLowerInvariant() ?? "";

            switch (first)
            {
                case "delete":
                    return HandleDeleteCommand(query);
                case "path":
                    return HandlePathCommand(query);
            }

            // Get all environment variables
            var envVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            var envKeys = new HashSet<string>(envVars.Keys.Cast<string>(), StringComparer.OrdinalIgnoreCase);

            bool foundMatch = false;
            foreach (System.Collections.DictionaryEntry entry in envVars)
            {
                var key = entry.Key.ToString();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                
                var value = entry.Value?.ToString();

                if (!string.IsNullOrEmpty(first) && !_context.API.FuzzySearch(first, key).Success && !_context.API.FuzzySearch(first, value).Success)
                {
                    continue;
                }

                foundMatch = true;
                if (!string.IsNullOrWhiteSpace(query.SecondSearch))
                {
                    results.Add(CreateResult(
                        $"Set '{key}' to '{query.SecondSearch}'",
                        value,
                        () =>
                        {
                            try
                            {
                                Environment.SetEnvironmentVariable(key, query.SecondSearch, EnvironmentVariableTarget.User);
                            }
                            catch
                            {
                                _context.API.ShowMsg(ClipboardErrorMsg);
                            }
                            return true;
                        }
                    ));
                }
                else
                {
                    results.Add(CreateResult(
                        key,
                        value,
                        () =>
                        {
                            try
                            {
                                _context.API.CopyToClipboard(value);
                            }
                            catch
                            {
                                _context.API.ShowMsg(ClipboardErrorMsg);
                            }
                            return true;
                        }
                    ));
                }
            }

            // If no match found and user provided both key and value, offer to add new variable
            if (!foundMatch && !string.IsNullOrWhiteSpace(query.FirstSearch) && !string.IsNullOrWhiteSpace(query.SecondSearch))
            {
                var newKey = query.FirstSearch.Trim();
                var newValue = query.SecondSearch.Trim();
                if (!envKeys.Contains(newKey))
                {
                    results.Add(CreateResult(
                        $"Add new environment variable '{newKey}'",
                        $"Set value to '{newValue}' (User scope)",
                        () =>
                        {
                            try
                            {
                                Environment.SetEnvironmentVariable(newKey, newValue, EnvironmentVariableTarget.User);
                            }
                            catch (Exception ex)
                            {
                                _context.API.ShowMsg(SetVarErrorMsg + ex.Message);
                            }
                            return true;
                        }
                    ));
                }
            }

            return results;
        }

        private Result CreateResult(string title, string subTitle, Func<bool> action = null, object contextData = null)
        {
            return new Result
            {
                Title = title,
                SubTitle = subTitle,
                Action = action != null ? _ => action() : (Func<ActionContext, bool>)null,
                IcoPath = IconPath,
                ContextData = contextData
            };
        }


        private List<Result> HandleDeleteCommand(Query query)
        {
            var results = new List<Result>();
            var key = query.SecondToEndSearch.Trim();
            if (!string.IsNullOrEmpty(key))
            {
                results.Add(CreateResult(
                    $"Delete environment variable '{key}'",
                    "Removes the variable from User scope",
                    () =>
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.User);
                        }
                        catch (Exception ex)
                        {
                            _context.API.ShowMsg(DeleteVarErrorMsg + ex.Message);
                        }
                        return true;
                    },
                    key
                ));
            }
            else
            {
                results.Add(CreateResult(
                    "Usage: delete KEY",
                    "Example: delete MY_VAR"
                ));
            }
            return results;
        }

        // Add this new method for handling PATH subcommands
        private List<Result> HandlePathCommand(Query query)
        {
            var results = new List<Result>();
            var pathValue = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            var pathEntries = pathValue.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var command = query.SecondSearch.ToLowerInvariant();
            var third = query.ThirdSearch.ToLowerInvariant();
            
            switch (command)
            {
                case "delete" or "del":
                {
                    if (string.IsNullOrWhiteSpace(third))
                    {
                        results.Add(CreateResult("Usage: path delete <path>", "Example: path delete C:\\MyFolder"));
                        return results;
                    }
                    
                    // Delete matching path entry
                    foreach (var entry in pathEntries)
                    {
                        if (_context.API.FuzzySearch(third, entry).Success)
                        {
                            results.Add(CreateResult(
                                $"Delete PATH entry: {entry}",
                                entry,
                                () =>
                                {
                                    try
                                    {
                                        var newPath = string.Join(System.IO.Path.PathSeparator,
                                            pathEntries.Where(e => !e.Equals(entry, StringComparison.OrdinalIgnoreCase)));
                                        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                                    }
                                    catch
                                    {
                                        _context.API.ShowMsg(DeleteVarErrorMsg + entry);
                                    }
                                    return true;
                                }
                            ));
                        }
                    }
                    if (results.Count == 0)
                    {
                        results.Add(CreateResult($"No PATH entry found matching '{third}'", null));
                    }

                    break;
                }
                case "add":
                    if (string.IsNullOrWhiteSpace(third))
                    {
                        results.Add(CreateResult("Usage: path add <path>", "Example: path add C:\\MyFolder"));
                        return results;
                    }
                    
                    // Append new path entry
                    results.Add(CreateResult(
                        $"Append '{third}' to PATH",
                        null,
                        () =>
                        {
                            try
                            {
                                var newPath = pathValue.TrimEnd(System.IO.Path.PathSeparator) + System.IO.Path.PathSeparator + third;
                                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                            }
                            catch
                            {
                                _context.API.ShowMsg(SetVarErrorMsg + "PATH");
                            }
                            return true;
                        }
                    ));

                    break;
                default:
                {
                    // List all PATH entries
                    foreach (var entry in pathEntries)
                    {
                        if (string.IsNullOrWhiteSpace(command) || _context.API.FuzzySearch(command, entry).Success)
                        {
                            results.Add(CreateResult(
                                entry,
                                "Copy to clipboard",
                                () =>
                                {
                                    try
                                    {
                                        _context.API.CopyToClipboard(entry);
                                    }
                                    catch
                                    {
                                        _context.API.ShowMsg(ClipboardErrorMsg);
                                    }
                                    return true;
                                }
                            ));
                        }
                    }
                    if (results.Count == 0)
                    {
                        results.Add(CreateResult($"No PATH entries found.", null));
                    }

                    break;
                }
            }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
        }
    }
}