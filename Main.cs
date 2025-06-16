using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        private List<Result> HandleAddCommand(Query query)
        {
            var results = new List<Result>();
            var key = query.SecondSearch.Trim();
            var value = query.ThirdSearch.Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                results.Add(CreateResult(
                    $"Add or update environment variable '{key}'",
                    $"Set value to '{value}' (User scope)",
                    () =>
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.User);
                        }
                        catch (Exception ex)
                        {
                            _context.API.ShowMsg(SetVarErrorMsg + ex.Message);
                        }
                        return true;
                    }
                ));
            }
            else
            {
                results.Add(CreateResult(
                    "Usage: add KEY VALUE",
                    "Example: add MY_VAR hello"
                ));
            }
            return results;
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

        public void Init(PluginInitContext context)
        {
            _context = context;
        }
    }
}