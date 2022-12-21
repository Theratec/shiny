//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//using CoreBluetooth;
//using Foundation;
//using Shiny.BluetoothLE.Internals;

//namespace Shiny.BluetoothLE;


//public class BleManager : AbstractBleManager
//{
//    const string ErrorCategory = "BluetoothLE";
//    readonly ManagerContext context;


//    public BleManager(ManagerContext context) => this.context = context;


//    public override bool IsScanning => this.context.Manager.IsScanning;


//    public override IObservable<AccessState> RequestAccess() => Observable.Create<AccessState>(ob =>
//    {

//    });


//    public override IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid)
//    {
//        var uuid = new NSUuid(peripheralUuid);
//        var peripheral = this.context
//            .Manager
//            .RetrievePeripheralsWithIdentifiers(uuid)
//            .FirstOrDefault();

//        if (peripheral == null)
//            return Observable.Return<IPeripheral?>(null);

//        var device = this.context.GetPeripheral(peripheral);
//        return Observable.Return(device);
//    }


//    public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null)
//    {
//        if (serviceUuid == null)
//            return Observable.Return(this.context.GetConnectedDevices().ToList());

//        return Observable.Return(this
//            .context
//            .Manager
//            .RetrieveConnectedPeripherals(CBUUID.FromString(serviceUuid))
//            .Select(x => this.context.GetPeripheral(x))
//            .ToList()
//        );
//    }



//    public override IObservable<ScanResult> Scan(ScanConfig? config = null) => Observable.Create<ScanResult>(ob =>
//    {
//        if (this.IsScanning)
//            throw new ArgumentException("There is already an existing scan");

//        config ??= new ScanConfig();
//        var sub = this.RequestAccess()
//            .Do(access =>
//            {
//                if (access != AccessState.Available)
//                    throw new PermissionException(ErrorCategory, access);
//            })
//            .SelectMany(_ =>
//            {
//                if (config.ServiceUuids == null || config.ServiceUuids.Length == 0)
//                {
//                    this.context.Manager.ScanForPeripherals(
//                        null!,
//                        PeripheralScanningOptions
//                    );
//                }
//                else
//                {
//                    var uuids = config.ServiceUuids.Select(CBUUID.FromString).ToArray();
//                    this.context.Manager.ScanForPeripherals(uuids, PeripheralScanningOptions);
//                }
//                return this.context.ScanResultReceived;
//            })
//            .Subscribe(
//                x => ob.OnNext(x),
//                ex => ob.OnError(ex),
//                () => ob.OnCompleted()
//            );

//        return () =>
//        {
//            this.context.Manager.StopScan();
//            sub?.Dispose();
//        };
//    });


//    public override void StopScan() => this.context.Manager.StopScan();
//}