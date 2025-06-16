using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Env
{
    public class Env : IAsyncPlugin
    {
        private PluginInitContext _context;

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();
            var first = query.FirstSearch?.ToLowerInvariant() ?? "";

            switch (first)
            {
                case "add":
                    return HandleAddCommand(query);
                case "delete":
                    return HandleDeleteCommand(query);
            }

            // Get all environment variables
            var envVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);

            foreach (System.Collections.DictionaryEntry entry in envVars)
            {
                var key = entry.Key.ToString();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                
                var value = entry.Value?.ToString();

                if (!_context.API.FuzzySearch(first, key).Success && !_context.API.FuzzySearch(first, value).Success)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(query.SecondSearch))
                {
                    results.Add(
                        new Result
                        {
                            Title = $"Set '{key}' to '{query.SecondSearch}'",
                            SubTitle = value,
                            Action = _ =>
                            {
                                try
                                {
                                    Environment.SetEnvironmentVariable(key, query.SecondSearch, EnvironmentVariableTarget.User);
                                }
                                catch
                                {
                                    _context.API.ShowMsg("Failed to copy to clipboard.");
                                }

                                return true;
                            },
                            IcoPath = "icon.png"
                        }
                    );
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = key,
                        SubTitle = value,
                        Action = _ =>
                        {
                            try
                            {
                                _context.API.CopyToClipboard(value);
                            }
                            catch
                            {
                                _context.API.ShowMsg("Failed to copy to clipboard.");
                            }
                            return true;
                        },
                        IcoPath = "icon.png"
                    });
                }
            }

            return results;
        }

        private List<Result> HandleAddCommand(Query query)
        {
            var results = new List<Result>();
            var key = query.SecondSearch.Trim();
            var value = query.ThirdSearch.Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                results.Add(new Result
                {
                    Title = $"Add or update environment variable '{key}'",
                    SubTitle = $"Set value to '{value}' (User scope)",
                    Action = _ =>
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.User);
                        }
                        catch (Exception ex)
                        {
                            _context.API.ShowMsg($"Failed to set variable: {ex.Message}");
                        }
                        return true;
                    },
                    IcoPath = "icon.png"
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title = "Usage: add KEY VALUE",
                    SubTitle = "Example: add MY_VAR hello",
                    IcoPath = "icon.png"
                });
            }
            return results;
        }

        private List<Result> HandleDeleteCommand(Query query)
        {
            var results = new List<Result>();
            var key = query.SecondToEndSearch.Trim();
            if (!string.IsNullOrEmpty(key))
            {
                results.Add(new Result
                {
                    Title = $"Delete environment variable '{key}'",
                    SubTitle = "Removes the variable from User scope",
                    Action = _ =>
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.User);
                        }
                        catch (Exception ex)
                        {
                            _context.API.ShowMsg($"Failed to delete variable: {ex.Message}");
                        }
                        return true;
                    },
                    IcoPath = "icon.png",
                    ContextData = key
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title = "Usage: delete KEY",
                    SubTitle = "Example: delete MY_VAR",
                    IcoPath = "icon.png"
                });
            }
            return results;
        }

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            return Task.CompletedTask;
        }
    }
}