using Plugin.Media.Abstractions;
//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Linq;

using UIKit;
using Foundation;

namespace Plugin.Media
{
    /// <summary>
    /// Implementation for Media
    /// </summary>
    public class MediaImplementation : IMedia
    {
        /// <summary>
        /// Color of the status bar
        /// </summary>
        public static UIStatusBarStyle StatusBarStyle { get; set; }

        ///<inheritdoc/>
        public Task<bool> Initialize() => Task.FromResult(true);

        /// <summary>
        /// Implementation
        /// </summary>
        public MediaImplementation()
        {
            StatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;
            IsCameraAvailable = UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera);

            var availableCameraMedia = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.Camera) ?? new string[0];
            var avaialbleLibraryMedia = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary) ?? new string[0];

            foreach (string type in availableCameraMedia.Concat(avaialbleLibraryMedia))
            {
                if (type == TypeMovie)
                    IsTakeVideoSupported = IsPickVideoSupported = true;
                else if (type == TypeImage)
                    IsTakePhotoSupported = IsPickPhotoSupported = true;
            }
        }
        /// <inheritdoc/>
        public bool IsCameraAvailable { get; }

        /// <inheritdoc/>
        public bool IsTakePhotoSupported { get; }

        /// <inheritdoc/>
        public bool IsPickPhotoSupported { get; }

        /// <inheritdoc/>
        public bool IsTakeVideoSupported { get; }

        /// <inheritdoc/>
        public bool IsPickVideoSupported { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public MediaPickerController GetPickPhotoUI()
        {
            if (!IsPickPhotoSupported)
                throw new NotSupportedException();

            var d = new MediaPickerDelegate(null, UIImagePickerControllerSourceType.PhotoLibrary, null);
            return SetupController(d, UIImagePickerControllerSourceType.PhotoLibrary, TypeImage);
        }

        /// <summary>
        /// Picks a photo from the default gallery
        /// </summary>
        /// <returns>Media file or null if canceled</returns>
        public Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null)
        {
            if (!IsPickPhotoSupported)
                throw new NotSupportedException();

            CheckPhotoUsageDescription();

            var cameraOptions = new StoreCameraMediaOptions
            {
                PhotoSize = options?.PhotoSize ?? PhotoSize.Full,
                CompressionQuality = options?.CompressionQuality ?? 100
            };

            return GetMediaAsync(UIImagePickerControllerSourceType.PhotoLibrary, TypeImage, cameraOptions);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public MediaPickerController GetTakePhotoUI(StoreCameraMediaOptions options)
        {
            if (!IsTakePhotoSupported)
                throw new NotSupportedException();
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            VerifyCameraOptions(options);

            var d = new MediaPickerDelegate(null, UIImagePickerControllerSourceType.PhotoLibrary, options);
            return SetupController(d, UIImagePickerControllerSourceType.Camera, TypeImage, options);
        }

        /// <summary>
        /// Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options)
        {
            if (!IsTakePhotoSupported)
                throw new NotSupportedException();
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            CheckCameraUsageDescription();

            VerifyCameraOptions(options);

            return GetMediaAsync(UIImagePickerControllerSourceType.Camera, TypeImage, options);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public MediaPickerController GetPickVideoUI()
        {
            if (!IsPickVideoSupported)
                throw new NotSupportedException();

            var d = new MediaPickerDelegate(null, UIImagePickerControllerSourceType.PhotoLibrary, null);
            return SetupController(d, UIImagePickerControllerSourceType.PhotoLibrary, TypeMovie);
        }

        /// <summary>
        /// Picks a video from the default gallery
        /// </summary>
        /// <returns>Media file of video or null if canceled</returns>
        public Task<MediaFile> PickVideoAsync()
        {
            if (!IsPickVideoSupported)
                throw new NotSupportedException();


            CheckPhotoUsageDescription();

            return GetMediaAsync(UIImagePickerControllerSourceType.PhotoLibrary, TypeMovie);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public MediaPickerController GetTakeVideoUI(StoreVideoOptions options)
        {
            if (!IsTakeVideoSupported)
                throw new NotSupportedException();
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            VerifyCameraOptions(options);

            var d = new MediaPickerDelegate(null, UIImagePickerControllerSourceType.Camera, options);
            return SetupController(d, UIImagePickerControllerSourceType.Camera, TypeMovie, options);
        }

        /// <summary>
        /// Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            if (!IsTakeVideoSupported)
                throw new NotSupportedException();
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            CheckCameraUsageDescription();

            VerifyCameraOptions(options);

            return GetMediaAsync(UIImagePickerControllerSourceType.Camera, TypeMovie, options);
        }

        private UIPopoverController popover;
        private UIImagePickerControllerDelegate pickerDelegate;
        /// <summary>
        /// image type
        /// </summary>
        public const string TypeImage = "public.image";
        /// <summary>
        /// movie type
        /// </summary>
        public const string TypeMovie = "public.movie";

        private void VerifyOptions(StoreMediaOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (options.Directory != null && Path.IsPathRooted(options.Directory))
                throw new ArgumentException("options.Directory must be a relative path", "options");
        }

        private void VerifyCameraOptions(StoreCameraMediaOptions options)
        {
            VerifyOptions(options);
            if (!Enum.IsDefined(typeof(CameraDevice), options.DefaultCamera))
                throw new ArgumentException("options.Camera is not a member of CameraDevice");
        }

        private static MediaPickerController SetupController(MediaPickerDelegate mpDelegate, UIImagePickerControllerSourceType sourceType, string mediaType, StoreCameraMediaOptions options = null)
        {
            var picker = new MediaPickerController(mpDelegate);
            picker.MediaTypes = new[] { mediaType };
            picker.SourceType = sourceType;

            if (sourceType == UIImagePickerControllerSourceType.Camera)
            {
                picker.CameraDevice = GetUICameraDevice(options.DefaultCamera);
                picker.AllowsEditing = options?.AllowCropping ?? false;

                if (options.OverlayViewProvider != null)
                {
                    var overlay = options.OverlayViewProvider();
                    if (overlay is UIView)
                    {
                        picker.CameraOverlayView = overlay as UIView;
                    }
                }
                if (mediaType == TypeImage)
                {
                    picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo;
                }
                else if (mediaType == TypeMovie)
                {
                    StoreVideoOptions voptions = (StoreVideoOptions)options;

                    picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Video;
                    picker.VideoQuality = GetQuailty(voptions.Quality);
                    picker.VideoMaximumDuration = voptions.DesiredLength.TotalSeconds;
                }
            }

            return picker;
        }

        /// <summary>
        /// Gets the visible view controller.
        /// </summary>
        /// <returns>The visible view controller.</returns>
        UIViewController GetVisibleViewController()
        {
            var rootController = UIApplication.SharedApplication.KeyWindow.RootViewController;

            if (rootController.PresentedViewController == null)
                return rootController;

            if (rootController.PresentedViewController is UINavigationController)
            {
                return ((UINavigationController)rootController.PresentedViewController).VisibleViewController;
            }

            if (rootController.PresentedViewController is UITabBarController)
            {
                return ((UITabBarController)rootController.PresentedViewController).SelectedViewController;
            }

            return rootController.PresentedViewController;
        }

        private Task<MediaFile> GetMediaAsync(UIImagePickerControllerSourceType sourceType, string mediaType, StoreCameraMediaOptions options = null)
        {
           

            var viewController = GetVisibleViewController();


            MediaPickerDelegate ndelegate = new MediaPickerDelegate(viewController, sourceType, options);
            var od = Interlocked.CompareExchange(ref pickerDelegate, ndelegate, null);
            if (od != null)
                throw new InvalidOperationException("Only one operation can be active at at time");

            var picker = SetupController(ndelegate, sourceType, mediaType, options);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad && sourceType == UIImagePickerControllerSourceType.PhotoLibrary)
            {
                ndelegate.Popover = new UIPopoverController(picker);
                ndelegate.Popover.Delegate = new MediaPickerPopoverDelegate(ndelegate, picker);
                ndelegate.DisplayPopover();
            }
            else
                viewController.PresentViewController(picker, true, null);

            return ndelegate.Task.ContinueWith(t =>
            {
                if (popover != null)
                {
                    popover.Dispose();
                    popover = null;
                }

                Interlocked.Exchange(ref pickerDelegate, null);
                return t;
            }).Unwrap();
        }

        private static UIImagePickerControllerCameraDevice GetUICameraDevice(CameraDevice device)
        {
            switch (device)
            {
                case CameraDevice.Front:
                    return UIImagePickerControllerCameraDevice.Front;
                case CameraDevice.Rear:
                    return UIImagePickerControllerCameraDevice.Rear;
                default:
                    throw new NotSupportedException();
            }
        }

        private static UIImagePickerControllerQualityType GetQuailty(VideoQuality quality)
        {
            switch (quality)
            {
                case VideoQuality.Low:
                    return UIImagePickerControllerQualityType.Low;
                case VideoQuality.Medium:
                    return UIImagePickerControllerQualityType.Medium;
                default:
                    return UIImagePickerControllerQualityType.High;
            }
        }


        void CheckCameraUsageDescription()
        {
            var info = NSBundle.MainBundle.InfoDictionary;

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                if (!info.ContainsKey(new NSString("NSCameraUsageDescription")))
                    throw new UnauthorizedAccessException("On iOS 10 and higher you must set NSCameraUsageDescription in your Info.plist file to enable Authorization Requests for Camera access!");
            }
        }

        void CheckPhotoUsageDescription()
        {
            var info = NSBundle.MainBundle.InfoDictionary;

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                if (!info.ContainsKey(new NSString("NSPhotoLibraryUsageDescription")))
                    throw new UnauthorizedAccessException("On iOS 10 and higher you must set NSPhotoLibraryUsageDescription in your Info.plist file to enable Authorization Requests for Photo Library access!");
            }
        }
    }
}
