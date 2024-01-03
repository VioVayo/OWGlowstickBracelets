using OWML.ModHelper;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace GlowyBraceletMod
{
    public class GlowyBraceletMod : ModBehaviour
    {
        private GameObject braceletPrefab, glowPrefab, cratePrefab;
        private ScreenPrompt prompt;
        private Material glowyMaterial;
        private Transform[] parentTransforms;

        private bool glowing = false;

        private float maxTiltAngle = 15, maxCenterOffset = 0.15f;
        private (Vector3, Vector3) //the min positions are at the wrists, the max positions are further up the arm towards the elbow
            minMaxPosLFSuit = (new(-3.5f, -0.4f, 0.15f), new(-1, -0.5f, 0.2f)),
            minMaxPosRTSuit = (new(3.5f, 0.4f, 0.15f), new(1, 0.5f, -0.1f)),
            minMaxPosLF = (new(-3.5f, -0.35f, 0), new(-2.2f, -0.25f, 0.25f)),
            minMaxPosRT = (new(3.5f, 0.35f, 0.05f), new(2.2f, 0.25f, -0.25f));
        private Color[] colours = new[]
        {
            new Color(0.52f, 0.57f, 1.5f, 1), //blue
            new Color(1.5f, 0.53f, 0.71f, 1), //pinkish-red
            new Color(1.5f, 1.1f, 0.53f, 1), //yeller
            new Color(0.53f, 1.5f, 0.64f, 1), //greej
            new Color(1.5f, 0.71f, 0.53f, 1) //oarnch
        };

        //VR stuff
        private bool vREnabled;
        private Transform[] parentTransformsVR;
        private (Vector3, Vector3)
            minMaxPosLFSuitVR = (new(-0.004f, 0.001f, 0.012f), new(0.004f, 0.003f, -0.033f)),
            minMaxPosRTSuitVR = (new(0.004f, 0.001f, 0.012f), new(0.004f, 0.003f, -0.033f)),
            minMaxPosLFVR = (new(0.011f, 0.001f, 0.02f), new(-0.011f, 0.001f, -0.01f)),
            minMaxPosRTVR = (new(-0.011f, 0.001f, 0.02f), new(-0.011f, 0.001f, -0.01f));


        private void Start()
        {
            vREnabled = ModHelper.Interaction.ModExists("Raicuparta.NomaiVR");

            var bundle = ModHelper.Assets.LoadBundle("Assets/rave_bundle");
            braceletPrefab = bundle.LoadAsset<GameObject>("Assets/GlowstickAssets/bracelet.prefab");
            glowPrefab = bundle.LoadAsset<GameObject>("Assets/GlowstickAssets/glow.prefab");
            cratePrefab = bundle.LoadAsset<GameObject>("Assets/GlowstickAssets/crate.prefab");
            ReplaceShaders(braceletPrefab, cratePrefab);

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                glowing = false;

                if (loadScene != OWScene.SolarSystem) return;
                prompt ??= new ScreenPrompt(InputLibrary.interact, "<CMD> " + "Make Bracelet");
                glowyMaterial ??= Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(obj => obj.name == "Props_HEA_BlueLightbulb_mat");
                parentTransforms = GameObject.Find("Player_Body").GetComponentsInChildren<Transform>().Where(obj => obj.gameObject.name.Contains("_Arm_Elbow_Jnt")).ToArray();
                if (vREnabled) StartCoroutine(FindVRHands());

                var crate = Instantiate(cratePrefab);
                crate.transform.SetParent(GameObject.Find("Module_Supplies").transform);
                crate.transform.localPosition = new(2.15f, 1.97f, -1f);
                crate.transform.localEulerAngles = new(0, 12, 0);
                var receiver = crate.GetComponent<InteractReceiver>();
                receiver._screenPrompt = prompt;
                receiver.OnPressInteract += AddBracelet;
                receiver.OnPressInteract += receiver.GetComponent<OWAudioSource>().PlayOneShot;
                receiver.OnReleaseInteract += receiver.ResetInteraction;
            };
        }

        private void ReplaceShaders(params GameObject[] prefabs) //thank you JohnCorby
        {
            foreach (var prefab in prefabs)
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>())
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                var replacementShader = Shader.Find(material.shader.name);
                if (replacementShader == null) continue;

                if (material.renderQueue != material.shader.renderQueue)
                {
                    var renderType = material.GetTag("RenderType", false);
                    var renderQueue = material.renderQueue;
                    material.shader = replacementShader;
                    material.SetOverrideTag("RenderType", renderType);
                    material.renderQueue = renderQueue;
                }
                else material.shader = replacementShader;
            }
        }

        private IEnumerator FindVRHands()
        {
            yield return null;
            parentTransformsVR = new[] 
            { 
                GameObject.Find("VrLeftHand").transform.Find("skeletal_hand(Clone)"), 
                GameObject.Find("VrRightHand").transform.Find("skeletal_hand(Clone)") 
            };
        }

        private void AddBracelet()
        {
            var armIndex = Random.Range(0, parentTransforms.Length);
            var slide = Random.Range(0f, vREnabled ? 0.3f : 1f);
            var maxTilt = ((slide < 0.15f) ? 0.5f : 1) * maxTiltAngle;
            var maxOffset = ((slide < 0.15f) ? 0.5f : 1) * maxCenterOffset;

            var isOnLeft = parentTransforms[armIndex].gameObject.name.Contains("LF");
            var minPosSuit = isOnLeft ? minMaxPosLFSuit.Item1 : minMaxPosRTSuit.Item1;
            var maxPosSuit = isOnLeft ? minMaxPosLFSuit.Item2 : minMaxPosRTSuit.Item2;
            var minPos = isOnLeft ? minMaxPosLF.Item1 : minMaxPosRT.Item1;
            var maxPos = isOnLeft ? minMaxPosLF.Item2 : minMaxPosRT.Item2;

            var bracelet = Instantiate(braceletPrefab);
            bracelet.transform.SetParent(parentTransforms[armIndex]);
            bracelet.transform.rotation = Quaternion.FromToRotation(bracelet.transform.up, parentTransforms[armIndex].right) * bracelet.transform.rotation;
            bracelet.transform.rotation = Quaternion.AngleAxis(Random.Range(0, 360), bracelet.transform.up) * bracelet.transform.rotation;
            bracelet.transform.rotation = Quaternion.AngleAxis(Random.Range(-maxTilt, maxTilt), parentTransforms[armIndex].forward) * bracelet.transform.rotation;
            bracelet.transform.rotation = Quaternion.AngleAxis(Random.Range(-maxTilt, maxTilt), parentTransforms[armIndex].up) * bracelet.transform.rotation;
            //these are non-commutative operations

            var braceletComponent = bracelet.AddComponent<GlowyBracelet>();
            var offset = Random.Range(-maxOffset, maxOffset) * parentTransforms[armIndex].forward + Random.Range(-maxOffset, maxOffset) * parentTransforms[armIndex].up;
            braceletComponent.posSuit = minPosSuit + slide * (maxPosSuit - minPosSuit) + 0.5f * offset;
            braceletComponent.pos = minPos + slide * (maxPos - minPos) + offset;
            braceletComponent.scaleSuit = 15 + slide * 2;
            braceletComponent.scale = 9;
            braceletComponent.UpdatePosition(PlayerState.IsWearingSuit());

            var colour = colours[Random.Range(0, colours.Length)];
            bracelet.transform.Find("glowy").gameObject.GetComponent<MeshRenderer>().material = glowyMaterial;
            bracelet.transform.GetComponentInChildren<OWEmissiveRenderer>().SetEmissionColor(colour);

            if (vREnabled) AddVRBracelet(bracelet, isOnLeft, slide, offset, colour);

            if (!glowing)
            {
                Instantiate(glowPrefab, GameObject.Find("Player_Body").transform);
                glowing = true;
            }
        }

        private void AddVRBracelet(GameObject bracelet, bool isOnLeft, float slide, Vector3 offset, Color colour)
        {
            var armIndex = isOnLeft ? 0 : 1;
            var minPosSuit = isOnLeft ? minMaxPosLFSuitVR.Item1 : minMaxPosRTSuitVR.Item1;
            var maxPosSuit = isOnLeft ? minMaxPosLFSuitVR.Item2 : minMaxPosRTSuitVR.Item2;
            var minPos = isOnLeft ? minMaxPosLFVR.Item1 : minMaxPosRTVR.Item1;
            var maxPos = isOnLeft ? minMaxPosLFVR.Item2 : minMaxPosRTVR.Item2;

            var rot = Quaternion.FromToRotation((isOnLeft ? 1 : -1) * bracelet.transform.parent.up, parentTransformsVR[armIndex].right)
                * Quaternion.FromToRotation(bracelet.transform.parent.right, parentTransformsVR[armIndex].forward);

            var braceletVR = Instantiate(bracelet);
            braceletVR.transform.SetParent(parentTransformsVR[armIndex]);
            braceletVR.transform.localRotation = bracelet.transform.localRotation;
            braceletVR.transform.rotation = rot * braceletVR.transform.rotation;

            var braceletComponent = braceletVR.GetComponent<GlowyBracelet>();
            slide /= 0.3f;
            offset = (rot * offset) / 12;
            braceletComponent.posSuit = minPosSuit + slide * (maxPosSuit - minPosSuit) + 0.5f * offset;
            braceletComponent.pos = minPos + slide * (maxPos - minPos) + offset;
            braceletComponent.scaleSuit = 0.925f;
            braceletComponent.scale = 0.5f;
            braceletComponent.UpdatePosition(PlayerState.IsWearingSuit());

            braceletVR.transform.Find("glowy").gameObject.GetComponent<MeshRenderer>().material = glowyMaterial;
            braceletVR.transform.GetComponentInChildren<OWEmissiveRenderer>().SetEmissionColor(colour);
        }

        private class GlowyBracelet : MonoBehaviour
        {
            public Vector3 posSuit, pos;
            public float scaleSuit, scale;

            public void Start()
            {
                GlobalMessenger.AddListener("SuitUp", new Callback(OnSuitUp));
                GlobalMessenger.AddListener("RemoveSuit", new Callback(OnSuitOff));
            }

            public void OnDestroy()
            {
                GlobalMessenger.RemoveListener("SuitUp", new Callback(OnSuitUp));
                GlobalMessenger.RemoveListener("RemoveSuit", new Callback(OnSuitOff));
            }

            public void OnSuitUp() { UpdatePosition(true); }

            public void OnSuitOff() { UpdatePosition(false); }

            public void UpdatePosition(bool suited)
            {
                transform.localPosition = suited ? posSuit : pos;
                transform.localScale = (suited ? scaleSuit : scale) * Vector3.one;
            }
        }
    }
}
