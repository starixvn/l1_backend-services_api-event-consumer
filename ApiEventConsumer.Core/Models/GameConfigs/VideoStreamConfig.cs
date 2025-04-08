using Newtonsoft.Json;

namespace ApiEventConsumer.Core.Models.GameConfigs
{
	public class VideoStreamConfig
	{
        public VideoStreamConfig()
        {
            VideoInfos = new List<VideoInfo>();
        }

        public List<VideoInfo> VideoInfos { get; set; }

        public void PopulateFromJson(string jsonData)
        {
            var videoData = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonData);
            if (videoData != null)
            {
                VideoInfos = videoData.Select(videoInfo => new VideoInfo
                {
                    VideoName = videoInfo.Key,
                    NumberOfVideos = videoInfo.Value
                }).ToList();
            }
        }
    }

	public class VideoInfo
	{
		public string? VideoName { get; set; }

		public int NumberOfVideos { get; set; }
	}
}
