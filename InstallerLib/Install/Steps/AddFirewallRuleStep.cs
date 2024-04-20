using InstallerLib.Utility;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InstallerLib.Install.Steps
{
    public class AddFirewallRuleStep : IInstallStep
    {
        public AddFirewallRuleStep(string name, string description, string programPath)
        {
            Name = name;
            Description = description;
            ProgramPath = programPath;
        }

        public string Name { get; }
        public string Description { get; set; }
        public string ProgramPath { get; }

        public Task Execute(IInstallerEngine engine)
        {
            // FirewallHelper.AddAuthorizedApplication(Name, Description, ProgramPath);
            // engine.Logger.LogInformation($"Adding firewall rule Name: '{Name}' Program: '{ProgramPath}'");
            return Task.CompletedTask;
        }
    }
}