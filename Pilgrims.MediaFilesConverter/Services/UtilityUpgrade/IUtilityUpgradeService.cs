using System;
using System.Threading;
using System.Threading.Tasks;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Interface for utility upgrade services
    /// </summary>
    public interface IUtilityUpgradeService
    {
        /// <summary>
        /// Gets information about the current utility installation
        /// </summary>
        Task<UtilityInfo> GetUtilityInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an update is available for the utility
        /// </summary>
        Task<UpdateCheckResult> IsUpdateAvailableAsync();

        /// <summary>
        /// Upgrades the utility to the latest version
        /// </summary>
        Task<UpgradeResult> UpgradeAsync(IProgress<UpgradeProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the name of the utility
        /// </summary>
        string UtilityName { get; }
    }
}