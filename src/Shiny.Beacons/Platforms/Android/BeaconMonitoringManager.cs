using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.Stores;
using System.Linq;
using Shiny.Locations;
using P = Android.Manifest.Permission;

namespace Shiny.Beacons;


public partial class BeaconMonitoringManager : IBeaconMonitoringManager, IShinyStartupTask
{
    readonly IRepository<BeaconRegion> repository;
    readonly IBleManager bleManager;
    readonly AndroidPlatform platform;


    public BeaconMonitoringManager(
        IBleManager bleManager,
        IRepository<BeaconRegion> repository,
        AndroidPlatform platform
    )
    {
        this.bleManager = bleManager;
        this.repository = repository;
        this.platform = platform;
    }


    public void Start()
    {
        var regions = this.GetMonitoredRegions();
        if (regions.Any())
            this.StartService();
    }


    public async Task StartMonitoring(BeaconRegion region)
    {
        (await this.RequestAccess()).Assert();

        this.repository.Set(region);
        this.StartService();
    }


    public void StopMonitoring(string identifier)
    {
        var region = this.repository.Get(identifier);

        if (region != null)
        {
            this.repository.Remove(identifier);
            var regions = this.repository.GetList();

            if (regions.Count == 0)
                this.StopService();
        }
    }


    public void StopAllMonitoring()
    {
        this.repository.Clear();
        this.StopService();
    }


    public async Task<AccessState> RequestAccess()
    {
        var access = await this.bleManager
            .RequestAccess()
            .ToTask()
            .ConfigureAwait(false);

        if (access == AccessState.Available)
        {
            var result = await this.platform
                .RequestFilteredPermissions(
                    new AndroidPermission(P.ForegroundService, 29, null)
                    //new(AndroidPermissions.PostNotifications, 33, null)
                )
                .ToTask();

            if (!result.IsSuccess())
                access = AccessState.Denied;
        }
        return access;
    }


    public IList<BeaconRegion> GetMonitoredRegions()
        => this.repository.GetList();


    void StartService()
    {
        if (OperatingSystemShim.IsAndroidVersionAtLeast(29) && !ShinyBeaconMonitoringService.IsStarted)
            this.platform.StartService(typeof(ShinyBeaconMonitoringService));
    }


    void StopService()
    {
        if (OperatingSystemShim.IsAndroidVersionAtLeast(29) && ShinyBeaconMonitoringService.IsStarted)
            this.platform.StopService(typeof(ShinyBeaconMonitoringService));
    }
}

