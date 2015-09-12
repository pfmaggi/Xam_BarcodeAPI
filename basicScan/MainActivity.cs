using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.Barcode;
using System.Threading.Tasks;

namespace basicScan
{
    [Activity(Label = "basicScan", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, EMDKManager.IEMDKListener, Scanner.IDataListener, Scanner.IStatusListener
    {
        private EMDKManager mEmdkManager = null;
        private BarcodeManager mBarcodeManager = null;
        private static Scanner mScanner = null;
        private static TextView mStatusTextView = null;
        private static EditText mDataView = null;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            mStatusTextView = FindViewById<TextView>(Resource.Id.textViewStatus);
            mDataView = FindViewById<EditText>(Resource.Id.editText1);

            // Now the EMDKManager!
            EMDKResults result = EMDKManager.GetEMDKManager(Application.Context, this);

            if (result.StatusCode != EMDKResults.STATUS_CODE.Success) {
                mStatusTextView.Text = "EMDKManager Request failed!";
            }
        }

        protected override void OnDestroy()
        {
            if (mEmdkManager != null)
            {
                mEmdkManager.Release();
                mEmdkManager = null;
            }

            base.OnDestroy();
        }

        protected override void OnStop()
        {
            base.OnStop();
            try
            {
                if (mScanner != null)
                {
                    mScanner.Disable();
                    mScanner = null;
                }
            }
            catch (ScannerException e)
            {
                e.PrintStackTrace();
            }
        }

        public void OnClosed()
        {
            if (mEmdkManager != null)
            {
                mEmdkManager.Release();
                mEmdkManager = null;
            }
        }

        public void OnOpened(EMDKManager emdkManager)
        {
            mEmdkManager = emdkManager;

            if (null == mScanner)
            {
                // Get Barcode Manager
                mBarcodeManager = (BarcodeManager)mEmdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);
                if (null == mBarcodeManager)
                {
                    mStatusTextView.Text = "Impossible to get BarcodeManager handle";
                    return;
                }
                mScanner = mBarcodeManager.GetDevice(BarcodeManager.DeviceIdentifier.InternalImager1);

                if (null == mScanner)
                {
                    mStatusTextView.Text = "Impossible to get Scanner handle";
                    return;
                }

                try
                {
                    mScanner.AddDataListener(this);
                    mScanner.AddStatusListener(this);

                    mScanner.TriggerType = Scanner.TriggerTypes.Hard;

                    mScanner.Enable();

                    mScanner.Read(); // Start an Async scann
                }
                catch (ScannerException e) 
                {
                    e.PrintStackTrace();
                }

                Toast.MakeText(this, "Press Hard Scan Button to start scanning...", ToastLength.Long).Show();
            }
        }

        public void OnData(ScanDataCollection scanDataCollection)
        {
            Task.Run(() =>
            {

                var scanDatas = scanDataCollection.GetScanData();

                foreach (ScanDataCollection.ScanData data in scanDatas)
                {
                    String barcodeData = data.Data;
                    ScanDataCollection.LabelType labelType = data.LabelType;

                    RunOnUiThread(() => { mDataView.Append(barcodeData + " " + labelType + "\n"); });
                }
            });
        }

        public void OnStatus(StatusData statusData)
        {
            String strStatus = "";

            Task.Run(() =>
            {

                if (StatusData.ScannerStates.Idle == statusData.State)
                {
                    strStatus = "Scanner enabled and idle";

                    try
                    {
                        Task.Delay(100).Wait();
                        mScanner.Read();
                    }
                    catch (ScannerException e)
                    {
                        e.PrintStackTrace();
                    }
                } 
                else if (StatusData.ScannerStates.Scanning == statusData.State) 
                {
                    strStatus = "Scanner is Scanning...";
                }
                else if (StatusData.ScannerStates.Waiting == statusData.State)
                {
                    strStatus = "Scanner is Waiting...";

                }
                else if (StatusData.ScannerStates.Disabled == statusData.State)
                {
                    strStatus = "Scanner is Disabled...";

                }
                else if (StatusData.ScannerStates.Error == statusData.State)
                {
                    strStatus = "Scanner Error...";

                }

                RunOnUiThread(() => { mStatusTextView.Text = strStatus; });
            });
        }
    }
}

