﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accelerider.Windows.Core.DownloadEngine;
using Accelerider.Windows.Core.Files.OneDrive;
using Accelerider.Windows.Core.Tools;
using Accelerider.Windows.Infrastructure;
using Accelerider.Windows.Infrastructure.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Accelerider.Windows.Core.NetWork.UserModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OneDriveUser : NetDiskUserBase, IOneDriveUser, ITaskCreator
    {
        [JsonProperty("totalQuota")]
        private long _totalQuota;

        [JsonProperty("usedQuota")]
        private long _usedQuota;

        //private string _username;

        [JsonProperty("name")]
        public override string Username { get; protected set; }
        public override DataSize TotalCapacity => new DataSize(_totalQuota);
        public override DataSize UsedCapacity => new DataSize(_usedQuota);

        [JsonProperty("id")]
        public override string UserId { get; protected set; }

        internal AcceleriderUser AccUser { get; set; }


        public override async Task<bool> RefreshUserInfoAsync()
        {
            return true;
        }

        public override ITransferTaskToken UploadAsync(FileLocation from, FileLocation to)
        {
            throw new NotImplementedException();
        }

        public override Task<(ShareStateCode, ISharedFile)> ShareAsync(IEnumerable<INetDiskFile> files, string password = null)
        {
            throw new NotImplementedException();
        }

        public override async Task<ILazyTreeNode<INetDiskFile>> GetNetDiskFileRootAsync()
        {
            await Task.Delay(100);
            var tree = new LazyTreeNode<INetDiskFile>(new OneDriveFile { User = this })
            {
                ChildrenProvider = async parent =>
                {
                    if (parent.FileType != FileTypeEnum.FolderType) return null;
                    var json = JObject.Parse(
                        await new HttpClient().GetAsync(
                            $"http://api.usmusic.cn/onedrive/filelist?token={AccUser.Token}&user={UserId}&path={parent.FilePath.FullPath.UrlEncode()}"));
                    if (json.Value<int>("errno") != 0) return null;
                    return JArray.Parse(json["list"].ToString()).Select(v =>
                    {
                        var file = JsonConvert.DeserializeObject<OneDriveFile>(v.ToString());
                        file.User = this;
                        return file;
                    });
                }
            };
            return tree;
        }

        public override Task<IEnumerable<ISharedFile>> GetSharedFilesAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IDeletedFile>> GetDeletedFilesAsync()
        {
            throw new NotImplementedException();
        }


        public async Task<IReadOnlyCollection<string>> GetDownloadUrls(string file)
        {
            return await Task.Run(() => new[] { (GetNetDiskFileByPath(file) as OneDriveFile)?.DownloadLink });
        }

        public INetDiskFile GetNetDiskFileByPath(string path)
        {
            var fileName = path.Split('/').Last();
            var json = JObject.Parse(new HttpClient().Get(
                    $"http://api.usmusic.cn/onedrive/filelist?token={AccUser.Token}&user={UserId}&path={path.GetSuperPath().UrlEncode()}"));
            if (json.Value<int>("errno") != 0) return null;
            return JArray.Parse(json["list"].ToString()).Select(v =>
            {
                var file = JsonConvert.DeserializeObject<OneDriveFile>(v.ToString());
                file.User = this;
                return file;
            }).FirstOrDefault(v => v.FileName == fileName);
        }
    }
}
