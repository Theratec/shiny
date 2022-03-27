﻿using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Microsoft.Extensions.Logging;
using UIKit;


namespace Shiny.Locations
{
    public partial class GpsManager : IGpsManager, IShinyStartupTask
    {
        readonly CLLocationManager locationManager;
        readonly GpsManagerDelegate gdelegate;
        readonly ILogger logger;


        public GpsManager(ILogger<IGpsManager> logger)
        {
            this.logger = logger;
            this.gdelegate = new GpsManagerDelegate();
            this.locationManager = new CLLocationManager { Delegate = this.gdelegate };
        }


        public async void Start()
        {
            if (this.CurrentListener != null)
            {
                try
                {
                    // only auto-start if auth status was changed to FULL authorized, not restricted
                    if (this.locationManager.AuthorizationStatus == CLAuthorizationStatus.Authorized)
                        await this.StartListenerInternal(this.CurrentListener);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error trying to restart GPS");
                }
            }
        }


        public async Task<AccessState> RequestAccess(GpsRequest request)
        {
            var bg = request.BackgroundMode != GpsBackgroundMode.None;
            var status = await this.locationManager.RequestAccess(bg);

            if (status == AccessState.Available &&
                request.Precise &&
                UIDevice.CurrentDevice.CheckSystemVersion(14, 0) &&
                this.locationManager.AccuracyAuthorization != CLAccuracyAuthorization.FullAccuracy)
            {
                status = AccessState.Restricted;
            }

            return status;
        }


        public AccessState GetCurrentStatus(GpsRequest request)
            => this.locationManager.GetCurrentStatus(request.BackgroundMode != GpsBackgroundMode.None);


        GpsRequest? request;
        public GpsRequest? CurrentListener
        {
            get => this.request;
            set
            {
                var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
                if (bg == GpsBackgroundMode.None)
                    this.request = value;
                else
                    this.Set(ref this.request, value);
            }
        }


        public IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync<IGpsReading?>(async ct =>
        {
            (await this.RequestAccess(GpsRequest.Foreground)).Assert();
            if (this.locationManager.Location == null)
                return null;

            return new GpsReading(this.locationManager.Location);
        });


        public async Task StartListener(GpsRequest request)
        {
            if (this.CurrentListener != null)
                throw new ArgumentException("There is already an active GPS listener");

            await this.StartListenerInternal(request);
        }


        public Task StopListener()
        {
#if __IOS__
            this.locationManager.AllowsBackgroundLocationUpdates = false;
            //if (this.CurrentListener.MinimumDistance != null || this.CurrentListener.ThrottledInterval != nul)
            //this.locationManager.DisallowDeferredLocationUpdates();
#endif
            this.locationManager.StopUpdatingLocation();
            this.CurrentListener = null;

            return Task.CompletedTask;
        }


        protected virtual async Task StartListenerInternal(GpsRequest request)
        {
            (await this.RequestAccess(request).ConfigureAwait(false)).Assert();
            this.gdelegate.Request = request;
//#if __IOS__
//            this.locationManager.AllowsBackgroundLocationUpdates = request.BackgroundMode != GpsBackgroundMode.None;
//            //this.locationManager.ShowsBackgroundLocationIndicator = request.UseBackground;
//            var throttledInterval = request.ThrottledInterval?.TotalSeconds ?? 0;
//            var minDistance = request.MinimumDistance?.TotalMeters ?? 0;

//            if (request.BackgroundMode != GpsBackgroundMode.None && (throttledInterval > 0 || minDistance > 0))
//            {
//                this.locationManager.DistanceFilter = CLLocationDistance.FilterNone;
//                this.locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
//            }
//#endif
            switch (request.Priority)
            {
                // TODO: other accuracy values for iOS
                case GpsPriority.Highest:
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
                    break;

                case GpsPriority.Normal:
                    //CLActivityType.Airborne
                    //CLActivityType.AutomotiveNavigation
                    //CLActivityType.Fitness
                    //CLActivityType.OtherNavigation

                    //CLLocation.AccurracyBestForNavigation
                    //CLLocation.AccuracyHundredMeters;
                    //CLLocation.AccuracyKilometer
                    //CLLocation.AccuracyThreeKilometers
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyNearestTenMeters;
                    break;

                case GpsPriority.Low:
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyHundredMeters;
                    break;
            }

            // TODO: other iOS config
            //this.locationManager.ShouldDisplayHeadingCalibration
            //this.locationManager.ShowsBackgroundLocationIndicator
            //this.locationManager.PausesLocationUpdatesAutomatically = false;
            //this.locationManager.DisallowDeferredLocationUpdates
            //this.locationManager.ActivityType = CLActivityType.Airborne;
            //this.locationManager.LocationUpdatesPaused
            //this.locationManager.LocationUpdatesResumed
            //this.locationManager.Failed
            //this.locationManager.UpdatedHeading
            //if (CLLocationManager.HeadingAvailable)
            //    this.locationManager.StopUpdatingHeading();
            this.locationManager.StartUpdatingLocation();
            this.CurrentListener = request;
        }

        public IObservable<IGpsReading> WhenReading() => this.gdelegate.WhenGps();
    }
}
