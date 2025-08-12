using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            var first = query.FirstSearch?.ToLowerInvariant() ?? "";

            var envVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User).Cast<System.Collections.DictionaryEntry>().ToDictionary(entry => entry.Key.ToString() ?? "", entry => entry.Value?.ToString() ?? string.Empty);

            switch (first)
            {
                case "delete" or "del":
                    return HandleDeleteCommand(envVars, query);
                case "path":
                    return HandlePathCommand(query);
            }

            // Get all environment variables
            var results = FuzzySearch(envVars, first);

            return results
                .OrderBy(r => r.extraExactMatch ? 0 : 1) // Prioritize exact matches
                .Select(r =>
                {
                    if (r.extraExactMatch)
                    {
                        if (string.IsNullOrWhiteSpace(query.SecondSearch))
                        {
                            return CreateResult(
                                $"Exact value `{r.key}` not found",
                                $"Add a value to create it"
                            );
                        }
                        
                        return CreateResult(
                            $"Add new variable {first}='{query.SecondSearch}'",
                            $"Set value to '{query.SecondSearch}' (User scope)",
                            () =>
                            {
                                try
                                {
                                    Environment.SetEnvironmentVariable(first, query.SecondSearch, EnvironmentVariableTarget.User);
                                }
                                catch (Exception ex)
                                {
                                    _context.API.ShowMsg(SetVarErrorMsg + ex.Message);
                                }

                                return true;
                            }
                        );
                    }

                    if (!string.IsNullOrWhiteSpace(query.SecondSearch))
                    {
                        return CreateResult(
                            $"Set '{r.key}' to '{query.SecondSearch}'",
                            r.value,
                            () =>
                            {
                                try
                                {
                                    Environment.SetEnvironmentVariable(r.key, query.SecondSearch, EnvironmentVariableTarget.User);
                                }
                                catch
                                {
                                    _context.API.ShowMsg(ClipboardErrorMsg);
                                }

                                return true;
                            }
                        );
                    }

                    return CreateResult(
                        r.key,
                        $"Copy to clipboard: '{r.value}'",
                        () =>
                        {
                            try
                            {
                                _context.API.CopyToClipboard(r.value);
                            }
                            catch
                            {
                                _context.API.ShowMsg(ClipboardErrorMsg);
                            }

                            return true;
                        }
                    );
                }
            ).ToList();
        }

        record SearchResult(string key, string value, bool extraExactMatch = false);

        private List<SearchResult> FuzzySearch(IDictionary<string, string> items, string searchKey)
        {
            var results = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(searchKey))
            {
                return items.Select(item => new SearchResult(item.Key, item.Value)).ToList();
            }

            var foundExact = false;

            foreach (var (itemKey, itemValue) in items)
            {
                if (string.IsNullOrWhiteSpace(itemKey))
                {
                    continue;
                }

                if (!_context.API.FuzzySearch(searchKey, itemKey).Success && (!string.IsNullOrWhiteSpace(itemValue) && !_context.API.FuzzySearch(itemValue, searchKey).Success))
                {
                    continue;
                }

                results.Add(new SearchResult(itemKey, itemValue));

                if (itemKey.Equals(searchKey, StringComparison.OrdinalIgnoreCase))
                {
                    foundExact = true;
                }
            }

            if (!foundExact)
            {
                // If no exact match found, add a result for adding a new variable
                results.Add(new SearchResult(searchKey, "", true));
            }

            return results;
        }


        private Result CreateResult(string title, string? subTitle, Func<bool>? action = null, object? contextData = null)
        {
            return new Result
            {
                Title = title,
                SubTitle = subTitle ?? "",
                Action = action != null ? _ => action() : null,
                IcoPath = IconPath,
                ContextData = contextData
            };
        }


        private List<Result> HandleDeleteCommand(Dictionary<string, string> envVars, Query query)
        {
            var handleDeleteCommand = FuzzySearch(envVars, query.SecondToEndSearch.Trim()).Where(r => !r.extraExactMatch).Select(r => CreateResult(
                    $"Delete variable {r.key}",
                    r.value,
                    () =>
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable(r.key, null, EnvironmentVariableTarget.User);
                        }
                        catch
                        {
                            _context.API.ShowMsg(DeleteVarErrorMsg + r.key);
                        }

                        return true;
                    }
                )
            ).ToList();
            
            if (handleDeleteCommand.Count == 0)
            {
                handleDeleteCommand.Add(CreateResult(
                    $"no results for '{query.SecondToEndSearch}'",
                    ""
                ));
            }

            return handleDeleteCommand;

        }

        // Add this new method for handling PATH subcommands
        private List<Result> HandlePathCommand(Query query)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            var pathEntries = pathValue.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x, x => x);

            var command = (query.SecondSearch ?? "").ToLowerInvariant() switch
            {
                "add" or "append" => "add",
                "delete" or "del" => "delete",
                _ => ""
            };

            var search = command == "" ? (query.SecondSearch ?? "") : (query.ThirdSearch ?? "");

            return command switch
            {
                "delete" when search == "" => [CreateResult("Usage: path delete <path>", "Example: path delete C:\\MyFolder"),],
                "add" when search == "" => [CreateResult("Usage: path add <path>", "Example: path add C:\\MyFolder"),],
                "delete" => FuzzySearch(pathEntries, search).Where(x => !x.extraExactMatch).Select(r =>
                    {
                        return CreateResult(
                            $"Delete: {r.key}",
                            r.value,
                            () =>
                            {
                                try
                                {
                                    var newPath = string.Join(System.IO.Path.PathSeparator, pathEntries.Keys.Where(e => !e.Equals(r.key, StringComparison.OrdinalIgnoreCase)));
                                    Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                                }
                                catch
                                {
                                    _context.API.ShowMsg(DeleteVarErrorMsg + r.key);
                                }

                                return true;
                            }
                        );
                    }
                ).ToList(),
                "add" => [CreateResult(
                        $"Append '{search}' to PATH",
                        null,
                        () =>
                        {
                            try
                            {
                                var newPath = pathValue.TrimEnd(System.IO.Path.PathSeparator) + System.IO.Path.PathSeparator + search;
                                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                            }
                            catch
                            {
                                _context.API.ShowMsg(SetVarErrorMsg + "PATH");
                            }

                            return true;
                        }
                    ),],
                _ => FuzzySearch(pathEntries, search).Where(x => !x.extraExactMatch).Select(r =>
                    {
                        return CreateResult(
                            r.key,
                            $"{search} Copy to clipboard",
                            () =>
                            {
                                try
                                {
                                    _context.API.CopyToClipboard(r.key);
                                }
                                catch
                                {
                                    _context.API.ShowMsg(ClipboardErrorMsg);
                                }

                                return true;
                            }
                        );
                    }
                ).ToList()
            };
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
        }
    }
}