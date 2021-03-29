using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.CoreAudioApi;

namespace project_delfino
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var speechConfig = SpeechConfig.FromSubscription("nope", "nope");
            await FromMic(speechConfig);

            //GetDeviceIDs(@"C:\Users\spiff\source\repos\project-delfino\project-delfino\deviceIDs.txt");

            Console.ReadKey();
        }

        async static Task FromFile(SpeechConfig speechConfig)
        {
            string inputPath = @"delfino.wav"; //removed
            string outputPath = @"output.txt";
            string filename = inputPath.Substring(60);
            using var audioConfig = AudioConfig.FromWavFileInput(inputPath);
            using var recognizer = new SpeechRecognizer(speechConfig, "nl-NL", audioConfig);

            var result = await recognizer.RecognizeOnceAsync();
            _ = TranscriptToFile(result.Text, outputPath);
            Console.WriteLine($"RECOGNIZED: Text={result.Text}");
        }

        async static Task FromMic(SpeechConfig speechConfig)
        {
            using var audioConfigMic = AudioConfig.FromMicrophoneInput("nope");
            using var audioConfigSpeaker = AudioConfig.FromSpeakerOutput("nope");
            using var recognizer = new SpeechRecognizer(speechConfig, "nl-NL", audioConfigSpeaker);
            string outputPath = @"output.txt";

            var stopRecognition = new TaskCompletionSource<int>();

            recognizer.Recognizing += (s, e) =>
            {
                Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
            };

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                    TranscriptToFile(e.Result.Text, outputPath);
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you update the subscription info?");
                }

                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\n    Session stopped event.");
                stopRecognition.TrySetResult(0);
            };

            await recognizer.StartContinuousRecognitionAsync();

            Console.WriteLine("Transcriber running\nPress ESC to stop");

            // TODO: implement stop logic
        }

        public static bool TranscriptToFile(string text, string path)
        {
            bool status = false;
            try
            {
                using (var writer = new StreamWriter(path, true))
                {
                    writer.WriteLine($"RECOGNISED:\n" +
                        $"{text}");
                }
                status = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}\n\n{ex.StackTrace}");
            }
            return status;
        }

        static void GetDeviceIDs(string output)
        {
            using (StreamWriter writer = new StreamWriter(output))
            {
                writer.WriteLine($"=============================================\n" +
                        $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}T{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second} GMT+1\n" +
                        $"Available audio IO devices\n" +
                        $"=============================================");
                var enumerator = new MMDeviceEnumerator();
                foreach (var endpoint in
                         enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
                {
                    string device = $"{endpoint.FriendlyName} ({endpoint.ID})";
                    writer.WriteLine(device);
                    Console.WriteLine(device);
                }
                writer.WriteLine($"=============================================");
            }
        }
    }
}
