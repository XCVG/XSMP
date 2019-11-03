using System;
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

        private const string commandLine = "-i \"{0}\" -flags:a +bitexact -map_metadata -1 -acodec pcm_s16le -ar 44100 -ac 2 \"{1}\"";

        /// <summary>
        /// Transcodes a music file
        /// </summary>
        /// <returns>The path of the transcoded file</returns>
        public static async Task<string> GetFromCacheOrTranscodeAsync(string hash, string path) //TODO transcode options
        {
            string encodedHash = HashUtils.HexStringToBase64String(hash);
            string targetPath = Path.GetFullPath(Path.Combine(Config.CacheFolderPath, $"{encodedHash}.wav"));

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
            //nop for now, TODO impl

            //enumerate files, sort by access date, delete enough to get under the cache limit
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
}
