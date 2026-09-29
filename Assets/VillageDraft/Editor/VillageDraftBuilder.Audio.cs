using UnityEditor;
using UnityEngine;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private static void BuildAudio()
        {
            var audioObject = new GameObject("Village audio and feedback");
            audioObject.transform.SetParent(root, false);
            var cues = audioObject.AddComponent<AudioSource>();
            cues.playOnAwake = false;
            cues.spatialBlend = 0;
            cues.volume = .48f;
            var ambience = audioObject.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0;
            ambience.volume = .17f;
            var director = audioObject.AddComponent<VillageAudioDirector>();
            director.progressBoard = board;
            director.cueSource = cues;
            director.ambienceSource = ambience;
            director.actionCue = LoadClip("action_success.wav");
            director.venueCompleteCue = LoadClip("place_complete.wav");
            director.villageCompleteCue = LoadClip("village_complete.wav");
            director.ambienceLoop = LoadClip("village_ambience.wav");
        }

        private static AudioClip LoadClip(string name)
        {
            var path = "Assets/VillageDraft/Audio/" + name;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new System.InvalidOperationException("Audio clip has not imported: " + path);
            return clip;
        }
    }
}
