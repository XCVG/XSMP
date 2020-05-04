using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace XSMP.MediaDatabase
{

    //really not happy with how this is set up
    public static class MediaTranscoder
    {
        //very hacky, we'll clean this up later

        private const string waveCommandLine = "-i \"{0}\" -flags:a +bitexact -map_metadata -1 -acodec pcm_s16le -ar 44100 -ac 2 \"{1}\"";
        private const string vorbisCommandLine = "-i \"{0}\" -c:a libvorbis -qscale:a 5 -ar 44100 -ac 2 \"{1}\"";

        /// <summary>
        /// Transcodes a music file
        /// </summary>
        /// <returns>The path of the transcoded file</returns>
        public static async Task<string> GetFromCacheOrTranscodeAsync(string hash, string path, TranscodeFormat format)
        {
            string commandLine;
            string extension;
            switch (format)
            {
                case TranscodeFormat.Wave:
                    commandLine = waveCommandLine;
                    extension = "wav";
                    break;
                case TranscodeFormat.Vorbis:
                    commandLine = vorbisCommandLine;
                    extension = "ogg";
                    break;
                //case TranscodeFormat.MP3:
                //    break;
                default:
                    throw new NotSupportedException($"Specified format \"{format}\" is not supported for transcoding");
            }

            string encodedHash = HashUtils.HexStringToBase64String(hash);
            string targetPath = Path.GetFullPath(Path.Combine(Config.CacheFolderPath, $"{encodedHash}.{extension}"));

            if (File.Exists(targetPath))
            {
                Console.WriteLine($"[MediaTranscoder] Retrieving {hash} from cache ({targetPath})");

                //I guess this is a nop
            }
            else
            {
                Console.WriteLine($"[MediaTranscoder] Transcoding {hash} to cache ({targetPath})");

                string sourcePath = Path.GetFullPath(path);
                //invoke ffmpeg and wait for exit
                await Task.Run(() =>
                {
                    Process p = new Process();
                    p.StartInfo.Arguments = string.Format(commandLine, sourcePath, targetPath);
                    p.StartInfo.WorkingDirectory = Program.ProgramFolderPath;
                    p.StartInfo.FileName = "ffmpeg.exe";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardOutput = true;

                    p.Start();
                    p.WaitForExit();

                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd(); //ffmpeg seems to toss everything into stderr for some reason

                    //Console.WriteLine(output);
                    //Console.WriteLine(error);
                    //Console.WriteLine();

                    if (!File.Exists(targetPath))
                        throw new TranscodingFailedException(error);

                });
            }

            return targetPath;
        }

        /// <summary>
        /// Cleans up the media cache
        /// </summary>
        /// <returns></returns>
        public static void TrimCache()
        {
            if (UserConfig.Instance.MaximumCacheSize < 0)
                return; //no cache size limit

            //enumerate files, sort by access date, delete enough to get under the cache limit

            var files = Directory.EnumerateFiles(Config.CacheFolderPath);
            var sortedFiles = files.OrderBy(f => File.GetLastAccessTime(f));

            IList<string> filesList = sortedFiles.ToList();

            if (filesList.Count == 0)
                return;

            float totalSize = 0; //float because close enough is close enough

            foreach(var file in filesList)
            {
                totalSize += new FileInfo(file).Length / 1000000f; //we want size in MB
            }

            while(totalSize > UserConfig.Instance.MaximumCacheSize && filesList.Count > 0)
            {
                string file = filesList[filesList.Count - 1];
                filesList.RemoveAt(filesList.Count - 1);

                try
                {
                    float fileSize = new FileInfo(file).Length / 1000000f;
                    File.Delete(file);
                    totalSize -= fileSize;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[MediaTranscoder] Failed to delete file \"{file}\" ({e.GetType().Name}: {e.Message})");
                }
            }

        }

        /// <summary>
        /// Clears the media cache entirely
        /// </summary>
        public static void FlushCache()
        {
            var files = Directory.EnumerateFiles(Config.CacheFolderPath);
            foreach(var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine($"[MediaTranscoder] Failed to delete file \"{file}\" ({e.GetType().Name}: {e.Message})");
                }
            }

            Console.WriteLine($"[MediaTranscoder] Flushed media cache");
        }

    }

    public class TranscodingFailedException : Exception
    {
        public string EncoderOutput { get; private set; }

        public TranscodingFailedException(string encoderOutput)
        {
            EncoderOutput = encoderOutput;
        }

        public override string Message => "An error occurred in the transcoding process";
    }

    public enum TranscodeFormat
    {
        Undefined, Wave, Vorbis, MP3, Flac
    }

    public static class TranscodeFormatExtensions
    {
        public static string GetContentType(this TranscodeFormat format)
        {
            switch (format)
            {
                case TranscodeFormat.Wave:
                    return "audio/wav";
                case TranscodeFormat.Vorbis:
                    return "audio/ogg";
                case TranscodeFormat.MP3:
                    return "audio/mpeg";
                case TranscodeFormat.Flac:
                    return "audio/flac";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
