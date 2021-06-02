﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using st = System.Threading;

namespace AutoDetoursAgent
{
    public class Unpacker : Job
    {
        private Process malunpack = new Process();
        private Process tar = new Process();

        private String defaultPathZip = "C:\\Temp\\unpacked.zip";

        public Unpacker(Logger logger, WorkerTask wt) : base(logger, wt)
        {
        }

        public override void StartJob()
        {
            logger.Log("Starting unpacking...");

            // Run Syelog Deamon to extract logs from traceapi32
            malunpack.StartInfo.FileName = "C:\\Temp\\mal_unpack.exe";
            malunpack.StartInfo.Arguments = "/exe C:\\Temp\\sample.exe /dir C:\\Temp\\unpacked /timeout " + (workerTask.time * 1000);
            malunpack.Start();
            logger.Log("Unpacking started.");
        }

        public override void StopJob()
        {
            logger.Log("Stopping unpacking...");
            // Stop both processes
            if (!malunpack.HasExited)
                malunpack.Kill();
            logger.Log("Unpacking stopped.");
        }

        // Compress mal_unpack results
        public override void TreatResults()
        {
            logger.Log("Compressing results...");
            tar.StartInfo.FileName = "C:\\Program Files\\7-Zip\\7z.exe";
            tar.StartInfo.Arguments = "a " + defaultPathZip + " C:\\Temp\\unpacked";
            tar.Start();

            st.Thread.Sleep(3000);
            logger.Log("Results compressed.");
        }

        public override async Task<bool> SubmitResults(String workerId, HttpClient client)
        {
            logger.Log("Submitting results...");
            // Submit ZIP results to the API
            string url = "workers/" + workerId + "/submit_task/";
            HttpResponseMessage response = null;
            var byteArray = File.ReadAllBytes(defaultPathZip);
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(byteArray, 0, byteArray.Length), "results", "unpacked.zip");
            try
            {
                response = await client.PostAsync(url, form);
            }
            catch (Exception)
            {

            }

            if (response != null && response.IsSuccessStatusCode)
            {
                logger.Log("ZIP successfully submitted.");
                return true;
            }
            return false;
        }
    }
}