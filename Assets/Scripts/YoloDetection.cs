#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT
using Windows.UI.Xaml;
using HoloLensForCV;
using YoloRuntime;
#endif

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.WSA.Input;

using System.Linq;
using System.IO;

using DrawingUtils;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine.SceneManagement;


#if !UNITY_EDITOR
    using Windows.Storage.Streams;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using System.Threading.Tasks;
    using Windows.Storage.Pickers;
#endif
#endregion

// https://devblogs.microsoft.com/dotnet/ml-net-and-model-builder-at-net-conf-2019-machine-learning-for-net/?utm_source=vs_developer_news&utm_medium=referral
namespace YoloDetectionHoloLens
{
    // Using the hololens for cv .winmd file for runtime support
    // https://docs.unity3d.com/2018.4/Documentation/Manual/IL2CPP-WindowsRuntimeSupport.html
    public class YoloDetection : MonoBehaviour
    {

        #region UnityVariables
        public CvUtils.DeviceTypeUnity deviceType;
        // Gesture recognizer
        private GestureRecognizer _gestureRecognizer;

        // Texture handler for bounding boxes
        public DrawBoundingBoxes drawBoundingBoxes;

        private bool streamStarted;
        private bool goNextScene;
        private bool bbReceived;

  
        // Parameters for host connect
        // https://stackoverflow.com/questions/32876966/how-to-get-local-host-name-in-c-sharp-on-a-windows-10-universal-app
        // Connecting to desktop host IP, not the hololens... Get the IP of PC and retry with specified port 

        public Text myText;
        int _tapCount;
        string _input;
        private int time;

        // From Tiny YOLO string labels.
        private string[] _labels = {
            "person","bicycle","car","motorbike","aeroplane","bus","train","truck","boat","traffic light","fire hydrant","stop sign","parking meter",
             "bench","bird","cat","dog","horse","sheep","cow","elephant","bear","zebra","giraffe","backpack","umbrella","handbag","tie","suitcase",
             "frisbee","skis","snowboard","sports ball","kite","baseball bat","baseball glove","skateboard","surfboard","tennis racket","bottle",
             "wine glass","cup","fork","knife","spoon","bowl","banana","apple","sandwich","orange","broccoli","carrot","hot dog","pizza","donut",
             "cake","chair","sofa","pottedplant","bed","diningtable","toilet","tvmonitor","laptop","mouse","remote","keyboard","cell phone","microwave",
             "oven","toaster","sink","refrigerator","book","clock","vase","scissors","teddy bear","hair drier","toothbrush"
        };

        private bool _holoLensMediaFrameSourceGroupStarted;
        TcpListener listener;
        TcpClient client;
        Quaternion zcamera;
        
        //Enum of sensors in HL2
        public enum SensorTypeUnity
        {
            Undefined = -1,
            PhotoVideo = 0,
            ShortThrowToFDepth = 1,
            ShortThrowToFReflectivity = 2,
            LongThrowToFDepth = 3,
            LongThrowToFReflectivity = 4,
            VisibleLightLeftLeft = 5,
            VisibleLightLeftFront = 6,
            VisibleLightRightFront = 7,
            VisibleLightRightRight = 8,
            NumberOfSensorTypes = 9
        }
        // type of sensor we want to stream (pv camera)
        public SensorTypeUnity sensorTypePv;
        //counts number of frames
        private int countFrames;
        //path for saving txt file
        private string path;
        //txt writer
        private TextWriter writer;
        //position of recognized object
        private Vector3 recognizedPos;
        //current camera Transform
        private Transform cameraTransform;
        //for saving camera parameters when bounding box was detected
        private Transform cameraTransformCache;
        public GameObject Cube;
        /// <summary>
        /// The cursor object attached to the Main Camera
        /// </summary>
        internal GameObject cursor;

        /// <summary>
        /// The label used to display the analysis on the objects in the real world
        /// </summary>
        public GameObject label;

        /// <summary>
        /// The quad object hosting the imposed image captured
        /// </summary>
        private GameObject quad;

        /// <summary>
        /// Renderer of the quad object
        /// </summary>
        internal Renderer quadRenderer;
        //position of Cube
        private Vector3 cubePosition;
        //bounding box parameters
        BoundingBoxLocal boxLocal;
        #endregion

#if ENABLE_WINMD_SUPPORT
        // Required for media frame source initialization
        private MediaFrameSourceGroupType _selectedMediaFrameSourceGroupType = MediaFrameSourceGroupType.PhotoVideoCamera;
        private SensorFrameStreamer _sensorFrameStreamer;
        private SpatialPerception _spatialPerception;
        private HoloLensForCV.DeviceType _deviceType;
        private MediaFrameSourceGroup _holoLensMediaFrameSourceGroup;
        private SensorType _sensorType;
        List<BoundingBox> boundingBoxes;
#endif

        #region UnityMethods
        // Use this for initialization
        async void Start()
        {
            time = 0;
#if !UNITY_EDITOR
            await DeleteOldFrames(Windows.Storage.ApplicationData.Current.LocalFolder, "Frame");
#endif
            cubePosition.Set(0, 0, 0);
            recognizedPos.Set(0, 0, 0);
            countFrames = 0;
            streamStarted = false;
            goNextScene = false;
            bbReceived = false;
            //_pvFrameMaterial = pvFrameGo.GetComponent<MeshRenderer>().material;

            _holoLensMediaFrameSourceGroupStarted = false;
            // Create the gesture handler
            InitializeHandler();

            // Initialize the bounding box canvas
            drawBoundingBoxes.InitDrawBoundingBoxes();

            // Wait for media frame source groups to be initialized
            await StartHoloLensMediaFrameSourceGroup();

            path = Path.Combine(UnityEngine.Application.persistentDataPath, "boundingBoxData.csv");
            writer = File.CreateText(path);
            writer.WriteLine("Count,Time(Minute:Second:Millisecond),Tracked,Position,Orientation,");

        }

        void Update()
        {
            time = time + 1;
            cameraTransform = Camera.main.transform;
            myText.text = _input;

            if (time == 500)//> 300 && streamStarted==false)
            {
                StartCoroutine(showBB());
                streamStarted = true;
                StartSocketToGetBBs();
            }else if (time>500 && time % 35 == 0)
            {
                Cube.SetActive(true);
                Cube.transform.position = cubePosition;
            }

            zcamera = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            UpdateHoloLensMediaFrameSourceGroup();

            if (streamStarted == true && goNextScene == false)
            {
                ReceiveBBs();
            }
            else if (goNextScene == true)
            {
                StopHoloLensMediaFrameSourceGroup();


                SceneManager.LoadScene(1);

            }
        }

        IEnumerator LoadYourAsyncScene()
        {
            // Set the current Scene to be able to unload it later
            Scene currentScene = SceneManager.GetActiveScene();

            // The Application loads the Scene in the background at the same time as the current Scene.
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);

            // Wait until the last operation fully loads to return anything
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Move the GameObject (you attach this in the Inspector) to the newly loaded Scene
            SceneManager.MoveGameObjectToScene(Cube, SceneManager.GetSceneAt(1));
            // Unload the previous Scene
            SceneManager.UnloadSceneAsync(currentScene);
        }

        async void OnApplicationQuit()
        {
            await StopHoloLensMediaFrameSourceGroup();
        }
#endregion
#region BoundingBoxes
        // Connect to get Bounding Box parameters
        void StartSocketToGetBBs() {
#if ENABLE_WINMD_SUPPORT
            //_input = "Connecting to host socket.";

            listener = new TcpListener(IPAddress.Any, 9090);
            listener.Start();
            /*if (!listener.Pending())
             {
                _input="Connecting to host socket.1111";
             }*/

            client = listener.AcceptTcpClient();
#endif
        }
        /// <summary>
        /// Initialize and start the hololens media frame source groups
        /// </summary>
        /// <returns>Task result</returns>


        // Receive bounding boxes.
        void ReceiveBBs() {
#if ENABLE_WINMD_SUPPORT
            // Provides the underlying stream of data for network access.
            NetworkStream nwStream = client.GetStream();
            //buffer of bytes received
            byte[] buffer = new byte[client.ReceiveBufferSize];

            int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);
            string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            //parsing the message      
            var dataBufferArray = dataReceived.Split(':');
            countFrames=int.Parse(dataBufferArray[0]);
            
            /*if (dataBufferArray != null)
            {                
                //_input = "Received"+countFrames.ToString()+" "+dataBufferArray.Length.ToString()+"/"+client.ReceiveBufferSize.ToString();
                _input = dataReceived.ToString();
            }*/

            List<BoundingBox> boundingBoxes = new List<BoundingBox>();

            // Iterate across data buffer, which is the total number of 
            // elements in the buffer
            const int boxSize = 6;
   
            if (dataBufferArray[0] != "00")
            {
                
                var numBoxes = (int)((dataBufferArray.Length) / (float)boxSize);
                
                var heightFactor = 504 / 896;
                var topCorner = cameraTransform.position + cameraTransform.forward - cameraTransform.right / 2f + cameraTransform.up * heightFactor / 2f;
            
                //_input="TopCorner: "+topCorner.x.ToString()+":"+topCorner.y.ToString()+":"+topCorner.z.ToString()+":";

                
                for (var boxCount = 0; boxCount < numBoxes; boxCount++)
                {               
                    BoundingBox box = new BoundingBox
                    {
                        TopLabel = int.Parse( dataBufferArray[(boxCount * boxSize) + 0]), // TopLabel is int
                        X = float.Parse(dataBufferArray[(boxCount * boxSize) + 1]),
                        Y = float.Parse(dataBufferArray[(boxCount * boxSize) + 2]),

                        Height = float.Parse(dataBufferArray[(boxCount * boxSize) + 3]),
                        Width = float.Parse(dataBufferArray[(boxCount * boxSize) + 4]),
                        Confidence = float.Parse(dataBufferArray[(boxCount * boxSize) + 5])
                    };
                    
                    boxLocal=new BoundingBoxLocal{
                        left=float.Parse(dataBufferArray[(boxCount * boxSize) + 1]),
                        top=float.Parse(dataBufferArray[(boxCount * boxSize) + 2]),
                        width=float.Parse(dataBufferArray[(boxCount * boxSize) + 3]),
                        height=float.Parse(dataBufferArray[(boxCount * boxSize) + 4])
                    };
                    
                    bbReceived=true;
                    /////////////////////////////////
                    //quadRenderer = quad.GetComponent<Renderer>() as Renderer;
                    //Bounds quadBounds = quadRenderer.bounds;

                    // Position the label as close as possible to the Bounding Box of the prediction 
                    // At this point it will not consider depth
                    //lastLabelPlaced.transform.parent = quad.transform;
                    //lastLabelPlaced.transform.localPosition = CalculateBoundingBoxPosition(quadBounds, boxLocal);

                    // Set the tag text
                    //lastLabelPlacedText.text = _labels[int.Parse( dataBufferArray[(boxCount * boxSize) + 0])];

                    // Cast a ray from the user's head to the currently placed label, it should hit the object detected by the Service.
                    // At that point it will reposition the label where the ray HL sensor collides with the object,
                    // (using the HL spatial tracking)
                    ////////////////////////////////////
                    var center = GetCenterOfBB(boxLocal.left, boxLocal.top, boxLocal.width, boxLocal.height);
                    recognizedPos = topCorner + cameraTransform.right * (1.9f * center.x) - cameraTransform.up * (1.9f * center.y) * heightFactor; //cameraTransform.right * 2.285714285714286f * center.x - cameraTransform.up * 2.285714285714286f * center.y * heightFactor;
                    
                    //var labelPos = DoRaycastOnSpatialMap(cameraTransform, cameraTransform.forward);
                    RaycastHit objHitInfo;
                    if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out objHitInfo, 30.0f,   SpatialMapping.PhysicsRaycastMask))
                    {
                        _input="CNC Machine is detected.";
                        cubePosition = objHitInfo.point;
                    }
                    else{
                        _input="The place of CNC Machine is not detected. Come closer to the machine.";//Physics didn't raycast"+time.ToString()+". Double tap if the position is correct and dwell for a couple of seconds to place it.";
                    }
                    /*
                    if (labelPos != null)
                    {
                        cubePosition=labelPos.Value;//cameraTransform.forward;//new Vector3(labelPos.Value.x,labelPos.Value.y,1.0f-labelPos.Value.z);
                        _input=labelPos.Value.x.ToString()+"  "+labelPos.Value.y.ToString()+"  "+labelPos.Value.z.ToString();
                    }*/
                    //objHitInfo.point.x.ToString()+"  "+objHitInfo.point.y.ToString()+"  "+objHitInfo.point.z.ToString();
                    /////////////////////////////////////////////////////
                    /*Vector3 objDirection=CalculateBoundingBoxPosition(boxLocal);
                    
                    Debug.Log("Repositioning Label");
                    Vector3 headPosition = Camera.main.transform.position;
                    RaycastHit objHitInfo;
                    
                    Vector3 objDirection = lastLabelPlaced.position;
                    if (Physics.Raycast(headPosition, objDirection, out objHitInfo, 30.0f,   SpatialMapping.PhysicsRaycastMask))
                    {
                        Cube.transform.position = objHitInfo.point;
                    }*/
                    ///////////////////////////////////////
                    /*cameraTransformCache=cameraTransform;

                    writer.WriteLine(recognizedPos.x.ToString()+":"+recognizedPos.y.ToString()+":"+recognizedPos.z.ToString());
                    Vector3 headPosition = Camera.main.transform.position;
                    RaycastHit objHitInfo;
                    if(Physics.Raycast(headPosition,recognizedPos,out objHitInfo,30.0f, SpatialMapping.PhysicsRaycastMask)){
                        Cube.transform.position=objHitInfo.point;
                    }*/
                    
                    // var labelPos = DoRaycastOnSpatialMap(cameraTransform, recognizedPos);
                    // Add the filled box to list
                    boundingBoxes.Add(box);
                }
                //cursor.GetComponent<Renderer>().material.color = Color.green;
                //_input=recognizedPos.x.ToString()+"_____"+recognizedPos.y.ToString()+"___"+recognizedPos.z.ToString();
                //_input="Recognized Position:"+recognizedPos.x.ToString()+":"+recognizedPos.y.ToString()+":"+recognizedPos.z.ToString()+":";
            }
            else
            {
                // Draw the list of empty boxes to clear
                // prior elements
                boundingBoxes.Add(new BoundingBox()
                {
                    Confidence = 0,
                    Label = "",
                    Height = 0,
                    Width = 0,
                    X = 0,
                    Y = 0});
            }
#endif
        }

        //Visualize BBs
        IEnumerator showBB() {

            /*var heightFactor = 504 / 896;
           
            var topCorner = cameraTransform.position + cameraTransform.forward -
                            cameraTransform.right / 2f +
                            cameraTransform.up * heightFactor / 2f;
            _input = "Hi";
            if (bbReceived)
            {
                var center = GetCenterOfBB(boxLocal.top, boxLocal.left, boxLocal.width, boxLocal.height);
                
                recognizedPos = topCorner + cameraTransform.right * (2.285714285714286f * center.x) - cameraTransform.up * (2.285714285714286f * center.y) * heightFactor;


                var labelPos = DoRaycastOnSpatialMap(cameraTransform, recognizedPos);

                if (labelPos != null)
                {
                    Cube.transform.position = labelPos.Value;//cameraTransform.forward;//new Vector3(labelPos.Value.x,labelPos.Value.y,1.0f-labelPos.Value.z);
                }
                _input = labelPos.Value.x.ToString() + "  " + labelPos.Value.y.ToString() + "  " + labelPos.Value.z.ToString();//objHitInfo.point.x.ToString()+"  "+objHitInfo.point.y.ToString()+"  "+objHitInfo.point.z.ToString();
            }
            /*else {
                _input = "boxlocalnull";
            }*/
            //Cube.transform.position=cubePosition;*/
           
            //drawBoundingBoxes.DrawBoxes(boundingBoxes, zcamera);

            yield return new WaitForSeconds(0.03f);
        }
        static Vector2 GetCenterOfBB(float left, float top, float width, float height)
        {
            return new Vector2((float)(left + (0.5 * width)),
                (float)(top + (0.5 * height)));
        }

        private Vector3? DoRaycastOnSpatialMap(Transform cameraTransform,
                                       Vector3 recognitionCenterPos)
        {
            //_input = "why";
            RaycastHit hitInfo;
            if (SpatialMapping.Instance == null)
            {
                _input = "null";
            }

            if (SpatialMapping.Instance != null &&
                Physics.Raycast(cameraTransform.position,
                               (recognitionCenterPos - cameraTransform.position),
                    out hitInfo, 1.0f, SpatialMapping.PhysicsRaycastMask))
            {
                _input = "norm";
                return hitInfo.point;
            }
            else {
                _input = "not norm"+time.ToString();
            }
            return null;
        }

        #endregion
        #region HololensMediaFrameGroup
        async Task StartHoloLensMediaFrameSourceGroup()
        {
#if ENABLE_WINMD_SUPPORT
            // Plugin doesn't work in the Unity editor
            _input = "Initalizing spatial map.";

            Debug.Log("YoloDetection.Detection.StartHoloLensMediaFrameSourceGroup: Setting up sensor frame streamer");
            _sensorType = (SensorType)sensorTypePv;
            _sensorFrameStreamer = new SensorFrameStreamer();
            _sensorFrameStreamer.Enable(_sensorType);

            Debug.Log("YoloDetection.Detection.StartHoloLensMediaFrameSourceGroup: Setting up spatial perception");
            _spatialPerception = new SpatialPerception();

            Debug.Log("YoloDetection.Detection.StartHoloLensMediaFrameSourceGroup: Setting up the media frame source group");
            
            // Cast device type 
            _deviceType = (HoloLensForCV.DeviceType)deviceType;
            _holoLensMediaFrameSourceGroup = new MediaFrameSourceGroup(
                _selectedMediaFrameSourceGroupType,
                _spatialPerception,
                _deviceType,
                _sensorFrameStreamer);
            _holoLensMediaFrameSourceGroup.Enable(_sensorType);

            Debug.Log("YoloDetection.Detection.StartHoloLensMediaFrameSourceGroup: Starting the media frame source group");
            await _holoLensMediaFrameSourceGroup.StartAsync();
            _holoLensMediaFrameSourceGroupStarted = true;

            _input = "Look at the CNC machine.";
#endif
        }
        unsafe void UpdateHoloLensMediaFrameSourceGroup()
        {
#if ENABLE_WINMD_SUPPORT
            if (!_holoLensMediaFrameSourceGroupStarted ||
                _holoLensMediaFrameSourceGroup == null )
            {
                return;
            }            
            SensorFrame latestSensorFrame = _holoLensMediaFrameSourceGroup.GetLatestSensorFrame(_sensorType);

            SoftwareBitmap softwareBitmap=latestSensorFrame.SoftwareBitmap;
            //SaveSoftwareBitmapToFile(softwareBitmap);
#endif
        }
        async Task StopHoloLensMediaFrameSourceGroup()
        {
#if ENABLE_WINMD_SUPPORT
            if (_holoLensMediaFrameSourceGroup == null || 
                !_holoLensMediaFrameSourceGroupStarted)
            {
                return;
            }

            await _holoLensMediaFrameSourceGroup.StopAsync();
            _holoLensMediaFrameSourceGroup = null;
            _sensorFrameStreamer = null;
            _holoLensMediaFrameSourceGroupStarted = false;
#endif
        }
#endregion
#region ComImport
        // https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/imaging
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }
        #endregion

        #region Complementary Functions
        #if ENABLE_WINMD_SUPPORT
        // Delete Old Frames
        private static async Task DeleteOldFrames(StorageFolder folder, string fileNameStartsWith)
        {
            var files = (await folder.GetFilesAsync())
                .Where(p => p.DisplayName.StartsWith(fileNameStartsWith));
            //&& !exceptionFiles.Any(e => e.DisplayName == p.DisplayName));

            foreach (var file in files)
            {
                await file.DeleteAsync(StorageDeleteOption.Default);
            }
        }
        // Get byte array from software bitmap.
        // https://github.com/qian256/HoloLensARToolKit/blob/master/ARToolKitUWP-Unity/Scripts/ARUWPVideo.cs
        unsafe byte* GetByteArrayFromSoftwareBitmap(SoftwareBitmap sb)
        {
            if (sb == null)
                return null;

            SoftwareBitmap sbCopy = new SoftwareBitmap(sb.BitmapPixelFormat, sb.PixelWidth, sb.PixelHeight);
            Interlocked.Exchange(ref sbCopy, sb);
            using (var input = sbCopy.LockBuffer(BitmapBufferAccessMode.Read))
            using (var inputReference = input.CreateReference())
            {
                byte* inputBytes;
                uint inputCapacity;
                ((IMemoryBufferByteAccess)inputReference).GetBuffer(out inputBytes, out inputCapacity);
                return inputBytes;
            }
        }

        // Save softwarebitmap to jpg file
        private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap)
        {
            
            // Get the app's local folder.
            StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            // Create a new file in the current folder.
            // Raise an exception if the file already exists.
            
            string desiredName = "Frame"+countFrames.ToString()+".jpg";
            StorageFile newFile = await localFolder.CreateFileAsync(desiredName, CreationCollisionOption.ReplaceExisting);

            if (newFile == null)
            {
                // The user cancelled the picking operation
                return;
            }

            using (IRandomAccessStream stream = await newFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);

                // Set additional encoding parameters, if needed
                encoder.BitmapTransform.ScaledWidth = 896;
                encoder.BitmapTransform.ScaledHeight = 504;
                //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.IsThumbnailGenerated = true;

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                    switch (err.HResult)
                    {
                        case WINCODEC_ERR_UNSUPPORTEDOPERATION: 
                            // If the encoder does not support writing a thumbnail, then try again
                            // but disable thumbnail generation.
                            encoder.IsThumbnailGenerated = false;
                            break;
                        default:
                            throw;
                    }
                }

                if (encoder.IsThumbnailGenerated == false)
                {
                    await encoder.FlushAsync();
                }
            }            
        }

        #endif
        #endregion
        #region TapGestureHandler
        private void InitializeHandler()
        {
            // New recognizer class
            _gestureRecognizer = new GestureRecognizer();

            // Set tap as a recognizable gesture
            _gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap | GestureSettings.Hold);


            // Begin listening for gestures
            _gestureRecognizer.StartCapturingGestures();

            // Capture on gesture events with delegate handler
            _gestureRecognizer.Tapped += GestureRecognizer_Tapped;

            Debug.Log("Gesture recognizer initialized.");
        }

        public void GestureRecognizer_Tapped(TappedEventArgs obj)
        {
            // Connect to socket on tapped event
            _tapCount += obj.tapCount;
            Debug.LogFormat("OnTappedEvent: tapCount = {0}", _tapCount);
            CameraAndRecognizedPos.cameraTransform = cameraTransform;
            CameraAndRecognizedPos.recognizedPos = Cube.transform.position - cameraTransform.position;
            //go to next scene with eye tracking
            goNextScene = true;
        }
        // Close Gesture Handler
        void CloseHandler()
        {
            _gestureRecognizer.StopCapturingGestures();
            _gestureRecognizer.Dispose();
        }
#endregion

    }
    public abstract class CvUtils
    {
        // Enum for selection of device type
        public enum DeviceTypeUnity
        {
            HL1 = 0,
            HL2 = 1
        }
    }
}

public class BoundingBoxLocal
{
    public float left { get; set; }
    public float top { get; set; }
    public float width { get; set; }
    public float height { get; set; }
}



