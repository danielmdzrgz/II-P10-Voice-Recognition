using System.Collections.Generic;
using System.IO;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HuggingFace.API.Examples
{
    public class SpeechRecognition : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private TMP_Dropdown spiderSelector;
        [SerializeField] private TextMeshProUGUI text;

        private AudioClip clip;
        private byte[] bytes;
        private bool recording;
        private GameObject selectedSpider;

        public List<GameObject> spiders;

        private void Start()
        {
            startButton.onClick.AddListener(StartRecording);
            stopButton.onClick.AddListener(StopRecording);
            stopButton.interactable = false;

            spiderSelector.onValueChanged.AddListener(OnDropdownValueChanged);
            List<string> options = new List<string>();
            foreach (GameObject spider in spiders)
            {
                options.Add(spider.tag);
            }
            spiderSelector.AddOptions(options);
            selectedSpider = spiders[0];
        }

        private void Update()
        {
            if (recording && Microphone.GetPosition(null) >= clip.samples)
            {
                StopRecording();
            }
        }

        private void StartRecording()
        {
            text.color = Color.white;
            text.text = "Recording...";
            startButton.interactable = false;
            stopButton.interactable = true;
            clip = Microphone.Start(null, false, 10, 44100);
            recording = true;
        }

        private void StopRecording()
        {
            var position = Microphone.GetPosition(null);
            Microphone.End(null);
            var samples = new float[position * clip.channels];
            clip.GetData(samples, 0);
            bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
            recording = false;
            SendRecording();
        }

        private void SendRecording()
        {
            text.color = Color.yellow;
            text.text = "Sending...";
            stopButton.interactable = false;
            HuggingFaceAPI.AutomaticSpeechRecognition(bytes, response =>
            {
                if (response == " Chase" || response == " Chase." || response == " Chase!")
                {
                    text.color = Color.white;
                    text.text = response;
                    ChaseZombie();
                }
                else if (response == " Grow" || response == " Grow." || response == " Grow!")
                {
                    text.color = Color.white;
                    text.text = response;
                    GrowSpider();
                }
                else 
                {
                    text.color = Color.red;
                    text.text = "[ " + response + " ]: Instruction not recognized.";
                }
                startButton.interactable = true;
            }, error =>
            {
                text.color = Color.red;
                text.text = error;
                startButton.interactable = true;
            });
        }

        private void OnDropdownValueChanged(int index)
        {
            selectedSpider = spiders[index];
            Debug.Log("Selected spider: " + selectedSpider.name);
        }

        private void ChaseZombie()
        {
            Debug.Log("Chasing zombie...");
            Debug.Log(selectedSpider.name);
            if (selectedSpider == null)
            {
                return;
            }
            GameObject zombie = GameObject.FindGameObjectWithTag("Zombie");
            Vector3 zombiePosition = zombie.transform.position;
            selectedSpider.transform.LookAt(zombiePosition);
            Vector3 direction = (selectedSpider.transform.position - zombiePosition).normalized;
            selectedSpider.transform.Translate(direction * 500 * Time.deltaTime);            
        }

        private void GrowSpider()
        {
            Debug.Log("Growing spider...");
            if (selectedSpider == null)
            {
                return;
            }
            selectedSpider.transform.localScale *= 1.5f;
        }

        private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
        {
            using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(36 + samples.Length * 2);
                    writer.Write("WAVE".ToCharArray());
                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16);
                    writer.Write((ushort)1);
                    writer.Write((ushort)channels);
                    writer.Write(frequency);
                    writer.Write(frequency * channels * 2);
                    writer.Write((ushort)(channels * 2));
                    writer.Write((ushort)16);
                    writer.Write("data".ToCharArray());
                    writer.Write(samples.Length * 2);

                    foreach (var sample in samples)
                    {
                        writer.Write((short)(sample * short.MaxValue));
                    }
                }
                return memoryStream.ToArray();
            }
        }
    }
}