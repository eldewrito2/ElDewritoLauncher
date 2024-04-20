using System.Threading.Tasks;

namespace InstallerLib.Install
{
    public interface IInstallStep
    {
        public string Type => this.GetType().Name;

        Task Execute(IInstallerEngine engine);
    }
}