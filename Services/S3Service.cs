using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.AspNetCore.Http;
using S3TestAPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace S3TestAPI.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _client;
        private static string accessKey = "AKIAQMFRBNULFAGOT5NU";
        private static string accessSecret = "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";

        public S3Service(IAmazonS3 client) {
            _client = new AmazonS3Client(accessKey, accessSecret, Amazon.RegionEndpoint.USEast2);
        }

        public async Task<S3Response> CopyingObjectAsync(string bucketName, string folderFile, string destinationfolder) {
            try {
                using (_client) {
                    CopyObjectRequest request = new CopyObjectRequest();
                    request.SourceBucket = bucketName;
                    request.SourceKey = folderFile.Replace(@"\", "/");
                    request.DestinationBucket = bucketName;
                    request.DestinationKey = destinationfolder.Replace(@"\", "/");
                    CopyObjectResponse response = await _client.CopyObjectAsync(request);
                    return new S3Response {
                        Message = response.ResponseMetadata.RequestId,
                        Status = response.HttpStatusCode
                    };
                }
            } catch (AmazonS3Exception e) {
                return new S3Response {
                    Message = e.Message,
                    Status = e.StatusCode
                };
            }
        }

        public async Task<S3Response> CreateBucketAsync(string bucketName) {
            try {
                if (await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName) == false) {
                    var putBucketRequest = new PutBucketRequest {
                        BucketName = bucketName,
                        UseClientRegion = true
                    };
                    var response = await _client.PutBucketAsync(putBucketRequest);

                    return new S3Response {
                        Message = response.ResponseMetadata.RequestId,
                        Status = response.HttpStatusCode
                    };
                }
            } catch (AmazonS3Exception e) {
                return new S3Response {
                    Message = e.Message,
                    Status = e.StatusCode
                };
            } catch (Exception ex) {
                return new S3Response {
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                };
            }
            return new S3Response {
                Message = "Something went wrong...",
                Status = HttpStatusCode.InternalServerError
            };
        }

        public async Task<S3Response> CreateFolderAsync(string folderName, string bucketName) {
            try {
                string folderKey = "";
                if (await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName) == false) {
                    for (var i = 0; i <= folderName.Length - 1; i++)
                        folderKey += folderName[i] + "/";
                    // folderKey = folderKey & "/"    'end the folder name with "/"
                    PutObjectRequest request = new PutObjectRequest();
                    request.BucketName = bucketName;
                    request.StorageClass = S3StorageClass.Standard;
                    request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.None;
                    // request.CannedACL = S3CannedACL.BucketOwnerFullControl
                    request.Key = folderKey;
                    request.ContentBody = string.Empty;
                    await _client.PutObjectAsync(request);
                }
            } catch (AmazonS3Exception e) {
                return new S3Response {
                    Message = e.Message,
                    Status = e.StatusCode
                };
            } catch (Exception ex) {
                return new S3Response {
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                };
            }
            return new S3Response {
                Message = "Something went wrong...",
                Status = HttpStatusCode.InternalServerError
            };
        }

        public async Task<S3Response> DeleteFileFromFolderAsync(string bucketName, string foldername, string file) {
            try {
                if (await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName) == false) {
                    foldername = foldername.Replace(@"\", "/");
                    DeleteObjectRequest dor = new DeleteObjectRequest() { BucketName = bucketName, Key = string.Format("{0}/{1}", foldername, file) };
                    await _client.DeleteObjectAsync(dor);
                }
            } catch (AmazonS3Exception e) {
                return new S3Response {
                    Message = e.Message,
                    Status = e.StatusCode
                };
            } catch (Exception ex) {
                return new S3Response {
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                };
            }
            return new S3Response {
                Message = "Something went wrong...",
                Status = HttpStatusCode.InternalServerError
            };
        }

        public async Task<S3Response> DeleteFolderAsync(string bucketName, string foldername) {
            try {
                if (!foldername.EndsWith("/"))
                    foldername = foldername + "/";
                DeleteObjectRequest dor = new DeleteObjectRequest() { BucketName = bucketName, Key = foldername };
                var response = await _client.DeleteObjectAsync(dor);
                return new S3Response {
                    Message = response.ResponseMetadata.RequestId,
                    Status = response.HttpStatusCode
                };
            } catch (AmazonS3Exception e) {
                return new S3Response {
                    Message = e.Message,
                    Status = e.StatusCode
                };
            }
        }

        public async Task<S3Response> DownloadFileAsync(string bucketName, string folderName, string FileName) {
            string target = Path.GetTempPath();
            folderName = folderName.Replace(@"\", "/");
            try {
                if (await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName) == false) {
                    ListObjectsRequest request = new ListObjectsRequest() { BucketName = bucketName };
                    do {
                        ListObjectsResponse response = await _client.ListObjectsAsync(request);
                        for (int i = 1; i <= response.S3Objects.Count - 1; i++) {
                            S3Object entry = response.S3Objects[i];
                            if (entry.Key.Replace(folderName + "/", "") == FileName) {
                                GetObjectRequest objRequest = new GetObjectRequest() { BucketName = bucketName, Key = entry.Key };
                                GetObjectResponse objResponse = await _client.GetObjectAsync(objRequest);
                                await objResponse.WriteResponseStreamToFileAsync(target + FileName, true, CancellationToken.None);
                                break;
                            }
                        }
                        if ((response.IsTruncated))
                            request.Marker = response.NextMarker;
                        else
                            request = null/* TODO Change to default(_) if this is not a reference type */;
                    }
                    while (request != null);
                }
            } catch (AmazonS3Exception e) {
                return new S3Response {
                    Message = e.Message,
                    Status = e.StatusCode
                };
            } catch (Exception ex) {
                return new S3Response {
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                };
            }
            return new S3Response {
                Message = "Something went wrong...",
                Status = HttpStatusCode.InternalServerError
            };
        }

        public async Task<List<string>> FilesListAsync(string bucketName) {
            var listVersions = await _client.ListVersionsAsync(bucketName, CancellationToken.None);
            return listVersions.Versions.Select(c => c.Key).ToList();
        }

        public Task<Stream> GetFileAsync(string key) {
            throw new NotImplementedException();
        }

        public async Task<S3Response> GetFileFromFolderAsync(string bucketName, string folderName, string FileName, string target) {
            try {
                if (await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName) == false) {
                    ListObjectsRequest request = new ListObjectsRequest() { BucketName = bucketName };
                    do {
                        ListObjectsResponse response = await _client.ListObjectsAsync(request, CancellationToken.None);
                        for (int i = 1; i <= response.S3Objects.Count - 1; i++) {
                            S3Object entry = response.S3Objects[i];
                            if (entry.Key.Replace(folderName + "/", "") == FileName) {
                                GetObjectRequest objRequest = new GetObjectRequest() { BucketName = bucketName, Key = entry.Key };
                                GetObjectResponse objResponse = await _client.GetObjectAsync(objRequest);
                                await objResponse.WriteResponseStreamToFileAsync(target + FileName, true, CancellationToken.None);
                                break;
                            }
                        }
                        if ((response.IsTruncated))
                            request.Marker = response.NextMarker;
                        else
                            request = null/* TODO Change to default(_) if this is not a reference type */;
                    }
                    while (request != null); ;
                }
            } catch (AmazonS3Exception e) {
                return new S3Response {
                    Message = e.Message,
                    Status = e.StatusCode
                };
            } catch (Exception ex) {
                return new S3Response {
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                };
            }
            return new S3Response {
                Message = "Something went wrong...",
                Status = HttpStatusCode.InternalServerError
            };
        }

        public async Task<S3Response> OpenFileAsync(string bucketName, string folderName, string FileName) {
            var returnval = await DownloadFileAsync(bucketName, folderName, FileName);
            if (returnval.Message == "") {
                string target = Path.GetTempPath();
                System.Diagnostics.Process.Start(target + FileName);
            }
            return returnval;
        }

        public async Task<S3Response> UploadFileAsync(IFormFile file, string bucketNmae) {
            // get the file and convert it to the byte[]
            byte[] fileBytes = new Byte[file.Length];
            file.OpenReadStream().Read(fileBytes, 0, Int32.Parse(file.Length.ToString()));

            // create unique file name for prevent the mess
            var fileName = DateTime.Now.Ticks + file.FileName;

            PutObjectResponse response = null;

            using (var stream = new MemoryStream(fileBytes)) {
                var request = new PutObjectRequest {
                    BucketName = bucketNmae,
                    Key = fileName,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                response = await _client.PutObjectAsync(request);
            };

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK) {
                var jobName = await CreateMediaConverterJobForDASHAsync(fileName);
                //var jobName = await CreateMediaConverterJobAsync(fileName);

                // this model is up to you, in my case I have to use it following;
                return new S3Response {
                    Status = response.HttpStatusCode,
                    Message = "FileName: " + fileName + " " + jobName
                };
            } else {
                // this model is up to you, in my case I have to use it following;
                return new S3Response {
                    Status = response.HttpStatusCode,
                    Message = fileName
                };
            }
        }

        public async Task<S3Response> UploadFileToFolderAsync(IFormFile file, string bucketName, string folderName) {
            try {
                if (await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName) == false) {
                    await CreateFolderAsync(bucketName, folderName);
                } else {
                    byte[] fileBytes = new Byte[file.Length];
                    file.OpenReadStream().Read(fileBytes, 0, Int32.Parse(file.Length.ToString()));
                    // create unique file name for prevent the mess
                    var fileName = Guid.NewGuid() + file.FileName;
                    string key = string.Format("{0}/{1}", folderName, file.Name);
                    PutObjectRequest por = new PutObjectRequest();
                    using (var stream = new MemoryStream(fileBytes)) {
                        por.BucketName = bucketName;
                        por.StorageClass = S3StorageClass.Standard;
                        por.ServerSideEncryptionMethod = ServerSideEncryptionMethod.None;
                        por.CannedACL = S3CannedACL.PublicRead;
                        por.Key = key;
                        por.InputStream = stream;
                        var response = await _client.PutObjectAsync(por);
                        return new S3Response {
                            Message = response.ResponseMetadata.RequestId,
                            Status = response.HttpStatusCode
                        };
                    }
                }
            } catch (AmazonS3Exception e) {
                return new S3Response {
                    Message = e.Message,
                    Status = e.StatusCode
                };
            }
            return new S3Response {
                Message = "Something went wrong...",
                Status = HttpStatusCode.InternalServerError
            };
        }

        static async Task<string> CreateMediaConverterJobForDASHAsync(string fileName) {
            // TODO: bucket name to be change and regsion name also 

            string mediaConvertRole = "arn:aws:iam::026141093142:role/MediaConvert_Default_Role";
            string fileInput = "s3://testbucketmediaconverter/" + fileName;
            string fileOutput = "s3://testbucketmediaconverter/";
            // Once you know what your customer endpoint is, set it here
            string mediaConvertEndpoint = "";

            // If we do not have our customer-specific endpoint
            if (String.IsNullOrEmpty(mediaConvertEndpoint)) {
                // Obtain the customer-specific MediaConvert endpoint
                AmazonMediaConvertClient client = new AmazonMediaConvertClient(Amazon.RegionEndpoint.USEast2);
                DescribeEndpointsRequest describeRequest = new DescribeEndpointsRequest();
                DescribeEndpointsResponse describeResponse = await client.DescribeEndpointsAsync(describeRequest);
                mediaConvertEndpoint = describeResponse.Endpoints[0].Url;
            }

            // Since we have a service url for MediaConvert, we do not
            // need to set RegionEndpoint. If we do, the ServiceURL will
            // be overwritten
            AmazonMediaConvertConfig mcConfig = new AmazonMediaConvertConfig {
                ServiceURL = mediaConvertEndpoint,
            };

            AmazonMediaConvertClient mcClient = new AmazonMediaConvertClient(mcConfig);
            CreateJobRequest createJobRequest = new CreateJobRequest();

            createJobRequest.Role = mediaConvertRole;
            createJobRequest.UserMetadata.Add("Customer", "Amazon");

            #region Create job settings
            JobSettings jobSettings = new JobSettings();
            jobSettings.AdAvailOffset = 0;
            jobSettings.TimecodeConfig = new TimecodeConfig();
            jobSettings.TimecodeConfig.Source = TimecodeSource.ZEROBASED;
            createJobRequest.Settings = jobSettings;

            #region OutputGroup 1
            OutputGroup ofg = new OutputGroup();
            ofg.Name = "DASH ISO";
            ofg.CustomName = "test";
            ofg.OutputGroupSettings = new OutputGroupSettings();
            ofg.OutputGroupSettings.Type = OutputGroupType.DASH_ISO_GROUP_SETTINGS;
            ofg.OutputGroupSettings.DashIsoGroupSettings = new DashIsoGroupSettings();
            ofg.OutputGroupSettings.DashIsoGroupSettings.Destination = fileOutput;
            ofg.OutputGroupSettings.DashIsoGroupSettings.SegmentLength = 30;
            ofg.OutputGroupSettings.DashIsoGroupSettings.MinFinalSegmentLength = 0;
            ofg.OutputGroupSettings.DashIsoGroupSettings.FragmentLength = 2;
            ofg.OutputGroupSettings.DashIsoGroupSettings.SegmentControl = DashIsoSegmentControl.SINGLE_FILE;
            ofg.OutputGroupSettings.DashIsoGroupSettings.MpdProfile = DashIsoMpdProfile.MAIN_PROFILE;
            ofg.OutputGroupSettings.DashIsoGroupSettings.HbbtvCompliance = "NONE";

            Output output = new Output();
            output.NameModifier = "_output";
            output.Preset = "System-Ott_Dash_Mp4_Avc_16x9_1280x720p_30Hz_3.5Mbps";
            //output.Extension = ".mp4";

            ofg.Outputs.Add(output);
            createJobRequest.Settings.OutputGroups.Add(ofg);
            #endregion OutputGroup

            #region Input
            Input input = new Input();
            input.FilterEnable = InputFilterEnable.AUTO;
            input.PsiControl = InputPsiControl.USE_PSI;
            input.FilterStrength = 0;
            input.DeblockFilter = InputDeblockFilter.DISABLED;
            input.DenoiseFilter = InputDenoiseFilter.DISABLED;
            input.TimecodeSource = InputTimecodeSource.ZEROBASED;
            input.InputScanType = InputScanType.AUTO;
            input.FileInput = fileInput;

            AudioSelector audsel = new AudioSelector();
            audsel.Offset = 0;
            audsel.DefaultSelection = AudioDefaultSelection.DEFAULT;
            audsel.ProgramSelection = 1;
            //audsel.SelectorType = AudioSelectorType.TRACK;
            //audsel.Tracks.Add(1);
            input.AudioSelectors.Add("Audio Selector 1", audsel);

            input.VideoSelector = new VideoSelector();
            input.VideoSelector.ColorSpace = ColorSpace.FOLLOW;
            input.VideoSelector.Rotate = InputRotate.DEGREE_0;
            input.VideoSelector.AlphaBehavior = AlphaBehavior.DISCARD;

            createJobRequest.Settings.Inputs.Add(input);
            #endregion Input
            #endregion Create job settings

            try {
                CreateJobResponse createJobResponse = await mcClient.CreateJobAsync(createJobRequest);
                return "Job Id: " + createJobResponse.Job.Id;
            } catch (BadRequestException bre) {
                // If the enpoint was bad
                if (bre.Message.StartsWith("You must use the customer-")) {
                    // The exception contains the correct endpoint; extract it
                    mediaConvertEndpoint = bre.Message.Split('\'')[1];
                    return mediaConvertEndpoint;
                    // Code to retry query
                }
            }

            return null;
        }
        static async Task<string> CreateMediaConverterJobAsync(string fileName) {
            // TODO: bucket name to be change and regsion name also 

            string mediaConvertRole = "arn:aws:iam::026141093142:role/MediaConvert_Default_Role";
            string fileInput = "s3://sachinmediaconverter/" + fileName;
            string fileOutput = "s3://sachinmediaconverter/";
            // Once you know what your customer endpoint is, set it here
            string mediaConvertEndpoint = "";

            // If we do not have our customer-specific endpoint
            if (String.IsNullOrEmpty(mediaConvertEndpoint)) {
                // Obtain the customer-specific MediaConvert endpoint
                AmazonMediaConvertClient client = new AmazonMediaConvertClient(Amazon.RegionEndpoint.USEast2);
                DescribeEndpointsRequest describeRequest = new DescribeEndpointsRequest();
                DescribeEndpointsResponse describeResponse = await client.DescribeEndpointsAsync(describeRequest);
                mediaConvertEndpoint = describeResponse.Endpoints[0].Url;
            }

            // Since we have a service url for MediaConvert, we do not
            // need to set RegionEndpoint. If we do, the ServiceURL will
            // be overwritten
            AmazonMediaConvertConfig mcConfig = new AmazonMediaConvertConfig {
                ServiceURL = mediaConvertEndpoint,
            };

            AmazonMediaConvertClient mcClient = new AmazonMediaConvertClient(mcConfig);
            CreateJobRequest createJobRequest = new CreateJobRequest();

            createJobRequest.Role = mediaConvertRole;
            createJobRequest.UserMetadata.Add("Customer", "Amazon");

            #region Create job settings
            JobSettings jobSettings = new JobSettings();
            jobSettings.AdAvailOffset = 0;
            jobSettings.TimecodeConfig = new TimecodeConfig();
            jobSettings.TimecodeConfig.Source = TimecodeSource.ZEROBASED;
            createJobRequest.Settings = jobSettings;

            #region OutputGroup 1
            OutputGroup ofg = new OutputGroup();
            ofg.Name = "File Group";
            ofg.CustomName = "test";
            ofg.OutputGroupSettings = new OutputGroupSettings();
            ofg.OutputGroupSettings.Type = OutputGroupType.FILE_GROUP_SETTINGS;
            ofg.OutputGroupSettings.FileGroupSettings = new FileGroupSettings();
            ofg.OutputGroupSettings.FileGroupSettings.Destination = fileOutput;

            Output output = new Output();
            output.NameModifier = "_output";
            output.Preset = "System-Broadcast_Xdcam_Mxf_Mpeg2_Wav_16x9_1280x720p_60Hz_50Mbps";
            output.Extension = ".mp4";

            #region VideoDescription
            //VideoDescription vdes = new VideoDescription();
            //output.VideoDescription = vdes;
            //vdes.ScalingBehavior = ScalingBehavior.DEFAULT;
            //vdes.TimecodeInsertion = VideoTimecodeInsertion.DISABLED;
            //vdes.AntiAlias = AntiAlias.ENABLED;
            //vdes.Sharpness = 50;
            //vdes.AfdSignaling = AfdSignaling.NONE;
            //vdes.DropFrameTimecode = DropFrameTimecode.ENABLED;
            //vdes.RespondToAfd = RespondToAfd.NONE;
            //vdes.ColorMetadata = ColorMetadata.INSERT;
            //vdes.CodecSettings = new VideoCodecSettings();
            //vdes.CodecSettings.Codec = VideoCodec.H_264;
            //H264Settings h264 = new H264Settings();
            //h264.InterlaceMode = H264InterlaceMode.PROGRESSIVE;
            //h264.NumberReferenceFrames = 3;
            //h264.Syntax = H264Syntax.DEFAULT;
            //h264.Softness = 0;
            //h264.GopClosedCadence = 1;
            //h264.GopSize = 90;
            //h264.Slices = 1;
            //h264.GopBReference = H264GopBReference.DISABLED;
            //h264.SlowPal = H264SlowPal.DISABLED;
            //h264.SpatialAdaptiveQuantization = H264SpatialAdaptiveQuantization.ENABLED;
            //h264.TemporalAdaptiveQuantization = H264TemporalAdaptiveQuantization.ENABLED;
            //h264.FlickerAdaptiveQuantization = H264FlickerAdaptiveQuantization.DISABLED;
            //h264.EntropyEncoding = H264EntropyEncoding.CABAC;
            //h264.Bitrate = 5000000;
            //h264.FramerateControl = H264FramerateControl.SPECIFIED;
            //h264.RateControlMode = H264RateControlMode.CBR;
            //h264.CodecProfile = H264CodecProfile.MAIN;
            //h264.Telecine = H264Telecine.NONE;
            //h264.MinIInterval = 0;
            //h264.AdaptiveQuantization = H264AdaptiveQuantization.HIGH;
            //h264.CodecLevel = H264CodecLevel.AUTO;
            //h264.FieldEncoding = H264FieldEncoding.PAFF;
            //h264.SceneChangeDetect = H264SceneChangeDetect.ENABLED;
            //h264.QualityTuningLevel = H264QualityTuningLevel.SINGLE_PASS;
            //h264.FramerateConversionAlgorithm = H264FramerateConversionAlgorithm.DUPLICATE_DROP;
            //h264.UnregisteredSeiTimecode = H264UnregisteredSeiTimecode.DISABLED;
            //h264.GopSizeUnits = H264GopSizeUnits.FRAMES;
            //h264.ParControl = H264ParControl.SPECIFIED;
            //h264.NumberBFramesBetweenReferenceFrames = 2;
            //h264.RepeatPps = H264RepeatPps.DISABLED;
            //h264.FramerateNumerator = 30;
            //h264.FramerateDenominator = 1;
            //h264.ParNumerator = 1;
            //h264.ParDenominator = 1;
            //output.VideoDescription.CodecSettings.H264Settings = h264;
            #endregion VideoDescription

            #region AudioDescription
            //AudioDescription ades = new AudioDescription();
            //ades.LanguageCodeControl = AudioLanguageCodeControl.FOLLOW_INPUT;
            //// This name matches one specified in the Inputs below
            //ades.AudioSourceName = "Audio Selector 1";
            //ades.CodecSettings = new AudioCodecSettings();
            //ades.CodecSettings.Codec = AudioCodec.AAC;
            //AacSettings aac = new AacSettings();
            //aac.AudioDescriptionBroadcasterMix = AacAudioDescriptionBroadcasterMix.NORMAL;
            //aac.RateControlMode = AacRateControlMode.CBR;
            //aac.CodecProfile = AacCodecProfile.LC;
            //aac.CodingMode = AacCodingMode.CODING_MODE_2_0;
            //aac.RawFormat = AacRawFormat.NONE;
            //aac.SampleRate = 48000;
            //aac.Specification = AacSpecification.MPEG4;
            //aac.Bitrate = 64000;
            //ades.CodecSettings.AacSettings = aac;
            //output.AudioDescriptions.Add(ades);
            #endregion AudioDescription

            #region Mp4 Container
            //output.ContainerSettings = new ContainerSettings();
            //output.ContainerSettings.Container = ContainerType.MP4;
            //Mp4Settings mp4 = new Mp4Settings();
            //mp4.CslgAtom = Mp4CslgAtom.INCLUDE;
            //mp4.FreeSpaceBox = Mp4FreeSpaceBox.EXCLUDE;
            //mp4.MoovPlacement = Mp4MoovPlacement.PROGRESSIVE_DOWNLOAD;
            //output.ContainerSettings.Mp4Settings = mp4;
            #endregion Mp4 Container

            ofg.Outputs.Add(output);
            createJobRequest.Settings.OutputGroups.Add(ofg);
            #endregion OutputGroup

            #region OutputGroup 2
            OutputGroup ofg1 = new OutputGroup();
            ofg1.Name = "File Group";
            ofg1.CustomName = "test1";
            ofg1.OutputGroupSettings = new OutputGroupSettings();
            ofg1.OutputGroupSettings.Type = OutputGroupType.FILE_GROUP_SETTINGS;
            ofg1.OutputGroupSettings.FileGroupSettings = new FileGroupSettings();
            ofg1.OutputGroupSettings.FileGroupSettings.Destination = fileOutput;

            Output output1 = new Output();
            output1.NameModifier = "_output1";
            output1.Preset = "System-Broadcast_Xdcam_Mxf_Mpeg2_Wav_16x9_1280x720p_60Hz_50Mbps";
            output1.Extension = ".mp4";

            #region VideoDescription
            //VideoDescription vdes = new VideoDescription();
            //output.VideoDescription = vdes;
            //vdes.ScalingBehavior = ScalingBehavior.DEFAULT;
            //vdes.TimecodeInsertion = VideoTimecodeInsertion.DISABLED;
            //vdes.AntiAlias = AntiAlias.ENABLED;
            //vdes.Sharpness = 50;
            //vdes.AfdSignaling = AfdSignaling.NONE;
            //vdes.DropFrameTimecode = DropFrameTimecode.ENABLED;
            //vdes.RespondToAfd = RespondToAfd.NONE;
            //vdes.ColorMetadata = ColorMetadata.INSERT;
            //vdes.CodecSettings = new VideoCodecSettings();
            //vdes.CodecSettings.Codec = VideoCodec.H_264;
            //H264Settings h264 = new H264Settings();
            //h264.InterlaceMode = H264InterlaceMode.PROGRESSIVE;
            //h264.NumberReferenceFrames = 3;
            //h264.Syntax = H264Syntax.DEFAULT;
            //h264.Softness = 0;
            //h264.GopClosedCadence = 1;
            //h264.GopSize = 90;
            //h264.Slices = 1;
            //h264.GopBReference = H264GopBReference.DISABLED;
            //h264.SlowPal = H264SlowPal.DISABLED;
            //h264.SpatialAdaptiveQuantization = H264SpatialAdaptiveQuantization.ENABLED;
            //h264.TemporalAdaptiveQuantization = H264TemporalAdaptiveQuantization.ENABLED;
            //h264.FlickerAdaptiveQuantization = H264FlickerAdaptiveQuantization.DISABLED;
            //h264.EntropyEncoding = H264EntropyEncoding.CABAC;
            //h264.Bitrate = 5000000;
            //h264.FramerateControl = H264FramerateControl.SPECIFIED;
            //h264.RateControlMode = H264RateControlMode.CBR;
            //h264.CodecProfile = H264CodecProfile.MAIN;
            //h264.Telecine = H264Telecine.NONE;
            //h264.MinIInterval = 0;
            //h264.AdaptiveQuantization = H264AdaptiveQuantization.HIGH;
            //h264.CodecLevel = H264CodecLevel.AUTO;
            //h264.FieldEncoding = H264FieldEncoding.PAFF;
            //h264.SceneChangeDetect = H264SceneChangeDetect.ENABLED;
            //h264.QualityTuningLevel = H264QualityTuningLevel.SINGLE_PASS;
            //h264.FramerateConversionAlgorithm = H264FramerateConversionAlgorithm.DUPLICATE_DROP;
            //h264.UnregisteredSeiTimecode = H264UnregisteredSeiTimecode.DISABLED;
            //h264.GopSizeUnits = H264GopSizeUnits.FRAMES;
            //h264.ParControl = H264ParControl.SPECIFIED;
            //h264.NumberBFramesBetweenReferenceFrames = 2;
            //h264.RepeatPps = H264RepeatPps.DISABLED;
            //h264.FramerateNumerator = 30;
            //h264.FramerateDenominator = 1;
            //h264.ParNumerator = 1;
            //h264.ParDenominator = 1;
            //output.VideoDescription.CodecSettings.H264Settings = h264;
            #endregion VideoDescription

            #region AudioDescription
            //AudioDescription ades = new AudioDescription();
            //ades.LanguageCodeControl = AudioLanguageCodeControl.FOLLOW_INPUT;
            //// This name matches one specified in the Inputs below
            //ades.AudioSourceName = "Audio Selector 1";
            //ades.CodecSettings = new AudioCodecSettings();
            //ades.CodecSettings.Codec = AudioCodec.AAC;
            //AacSettings aac = new AacSettings();
            //aac.AudioDescriptionBroadcasterMix = AacAudioDescriptionBroadcasterMix.NORMAL;
            //aac.RateControlMode = AacRateControlMode.CBR;
            //aac.CodecProfile = AacCodecProfile.LC;
            //aac.CodingMode = AacCodingMode.CODING_MODE_2_0;
            //aac.RawFormat = AacRawFormat.NONE;
            //aac.SampleRate = 48000;
            //aac.Specification = AacSpecification.MPEG4;
            //aac.Bitrate = 64000;
            //ades.CodecSettings.AacSettings = aac;
            //output.AudioDescriptions.Add(ades);
            #endregion AudioDescription

            #region Mp4 Container
            //output.ContainerSettings = new ContainerSettings();
            //output.ContainerSettings.Container = ContainerType.MP4;
            //Mp4Settings mp4 = new Mp4Settings();
            //mp4.CslgAtom = Mp4CslgAtom.INCLUDE;
            //mp4.FreeSpaceBox = Mp4FreeSpaceBox.EXCLUDE;
            //mp4.MoovPlacement = Mp4MoovPlacement.PROGRESSIVE_DOWNLOAD;
            //output.ContainerSettings.Mp4Settings = mp4;
            #endregion Mp4 Container

            ofg1.Outputs.Add(output1);
            createJobRequest.Settings.OutputGroups.Add(ofg1);
            #endregion OutputGroup

            #region Input
            Input input = new Input();
            input.FilterEnable = InputFilterEnable.AUTO;
            input.PsiControl = InputPsiControl.USE_PSI;
            input.FilterStrength = 0;
            input.DeblockFilter = InputDeblockFilter.DISABLED;
            input.DenoiseFilter = InputDenoiseFilter.DISABLED;
            input.TimecodeSource = InputTimecodeSource.ZEROBASED;
            input.InputScanType = InputScanType.AUTO;
            input.FileInput = fileInput;

            AudioSelector audsel = new AudioSelector();
            audsel.Offset = 0;
            audsel.DefaultSelection = AudioDefaultSelection.DEFAULT;
            audsel.ProgramSelection = 1;
            //audsel.SelectorType = AudioSelectorType.TRACK;
            //audsel.Tracks.Add(1);
            input.AudioSelectors.Add("Audio Selector 1", audsel);

            input.VideoSelector = new VideoSelector();
            input.VideoSelector.ColorSpace = ColorSpace.FOLLOW;
            input.VideoSelector.Rotate = InputRotate.DEGREE_0;
            input.VideoSelector.AlphaBehavior = AlphaBehavior.DISCARD;

            createJobRequest.Settings.Inputs.Add(input);
            #endregion Input
            #endregion Create job settings

            try {
                CreateJobResponse createJobResponse = await mcClient.CreateJobAsync(createJobRequest);
                return "Job Id: " + createJobResponse.Job.Id;
            } catch (BadRequestException bre) {
                // If the enpoint was bad
                if (bre.Message.StartsWith("You must use the customer-")) {
                    // The exception contains the correct endpoint; extract it
                    mediaConvertEndpoint = bre.Message.Split('\'')[1];
                    return mediaConvertEndpoint;
                    // Code to retry query
                }
            }

            return null;
        }
    }
}
