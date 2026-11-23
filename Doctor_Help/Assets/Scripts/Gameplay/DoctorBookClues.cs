using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DoctorHelp
{
    internal sealed class XrayClueTarget
    {
        public readonly string Id;
        public readonly SpriteRenderer[] Renderers;
        public XrayClueTarget(string id, GameObject root) { Id=id; Renderers=root.GetComponentsInChildren<SpriteRenderer>(true); }
    }

    [DefaultExecutionOrder(200)]
    public sealed class DoctorBookClues : MonoBehaviour
    {
        [SerializeField] private PatientSpawnPoint patients;
        [SerializeField] private ExaminationToolSelection selection;
        [SerializeField] private DiseaseReferencePopup popup;
        [SerializeField] private ExaminationClueCatalog catalog;
        [SerializeField] private BoneClueMask boneMask;
        [SerializeField] private SpriteRenderer book;
        [SerializeField] private GameObject unreadMark;
        [SerializeField] private RectTransform feedbackRoot;
        [SerializeField] private ClueFlight flightPrefab;
        [SerializeField] private ClueSpriteCapture spriteCapture;
        [SerializeField] private Sprite thermometerIcon, heartIcon;
        [SerializeField, Min(.05f)] private float xraySeconds=.5f;
        [SerializeField, Min(.1f)] private float heartbeatSeconds=2;
        [SerializeField, Range(0,.15f)] private float bookPulseScale=.03f;
        [SerializeField, Min(.1f)] private float bookPulsePeriod=1.2f;
        private readonly ConsultationClueJournal journal=new ConsultationClueJournal();
        private readonly List<XrayClueTarget> targets=new List<XrayClueTarget>();
        private readonly Dictionary<string,ContinuousClueHold> xrayHolds=new Dictionary<string,ContinuousClueHold>();
        private readonly List<Vector2> cracks=new List<Vector2>();
        private readonly HashSet<string> present=new HashSet<string>();
        private readonly List<(string id, int start, int length, string caption)> dialogueMatches = new List<(string, int, int, string)>();
        private string matchedDialogue;
        private PatientExaminationProfile patient;
        private NpcDialogue dialogue;
        private BoneFractureAppearance bone;
        private MagnifierLens lens;
        private Vector3 bookScale;
        private bool paused, unfocused;
        public event Action Changed;
        public bool IsDiscovered(string id)=>journal.Contains(id);
        public bool HasUnread=>journal.Unread;
        public ExaminationClueCatalog Catalog=>catalog;

        private void Awake()
        {
            if(book!=null)bookScale=book.transform.localScale;
            if(selection!=null&&selection.Magnifier!=null)lens=selection.Magnifier.GetComponent<MagnifierLens>();
            if(unreadMark!=null)unreadMark.SetActive(false);
        }
        private void SyncPatient()
        {
            var current=patients!=null?patients.CurrentPatient:null;
            if(!journal.Begin(current!=null?current.ConsultationId:null))return;
            patient=current;dialogue=patient!=null?patient.GetComponent<NpcDialogue>():null;bone=patient!=null?patient.GetComponent<BoneFractureAppearance>():null;
            present.Clear();targets.Clear();xrayHolds.Clear();cracks.Clear();
            matchedDialogue = null;
            dialogueMatches.Clear();
            if(feedbackRoot!=null)foreach(var flight in feedbackRoot.GetComponentsInChildren<ClueFlight>(true))Destroy(flight.gameObject);
            if(patient!=null&&patient.Definition!=null)
            {
                foreach(string id in NpcMedicalConditions.Resolve(patient.Definition.SicknessIds,patient.Profile.AllSickness))present.Add(id);
                if(patient.TryGetComponent<WormInfestationAppearance>(out var worm)&&worm.SpawnedWorm!=null)targets.Add(new XrayClueTarget(worm.ClueSymptomId,worm.SpawnedWorm.gameObject));
                if(patient.TryGetComponent<OrganAbnormalityAppearance>(out var organ)&&organ.ClueInstance!=null)targets.Add(new XrayClueTarget(organ.ClueSymptomId,organ.ClueInstance));
                if(patient.TryGetComponent<SymptomPrefabAppearance>(out var prefabs))prefabs.AppendClueTargets(targets);
                if(bone!=null&&bone.HasFracture&&boneMask!=null)boneMask.FindCracks(bone.ClueRenderer,bone.ClueOffset,cracks);
            }
            ResetNotice();Changed?.Invoke();
        }
        private bool CanObserve=>!paused&&!unfocused&&Time.timeScale>0&&patient!=null&&patient.HasArrived&&!patient.HasReceivedMedicine&&patients!=null&&!patients.IsDeparting&&selection!=null&&selection.IsAvailable&&(popup==null||!popup.IsOpen);
        private void LateUpdate()
        {
            SyncPatient();
            if(popup!=null&&popup.IsOpen)Acknowledge();
            if(CanObserve){ObserveDialogue();ObserveXray();}else ResetXrayHolds();
            if(book!=null)book.transform.localScale=bookScale*(journal.Unread?1+bookPulseScale*Mathf.Sin(Time.unscaledTime*Mathf.PI*2/bookPulsePeriod):1);
            if(unreadMark!=null&&unreadMark.activeSelf!=journal.Unread)unreadMark.SetActive(journal.Unread);
        }
        public void Acknowledge(){journal.Acknowledge();ResetNotice();}
        private void ResetNotice(){if(book!=null)book.transform.localScale=bookScale;if(unreadMark!=null)unreadMark.SetActive(false);}
        private void ResetXrayHolds(){foreach(var hold in xrayHolds.Values)hold.Reset();}
        private ContinuousClueHold Hold(string id){if(!xrayHolds.TryGetValue(id,out var hold))xrayHolds.Add(id,hold=new ContinuousClueHold());return hold;}
        private void ObserveXray()
        {
            if(!selection.IsUsingXray||lens==null||!lens.isActiveAndEnabled||!lens.IsConfigured){ResetXrayHolds();return;}
            foreach(var target in targets)
            {
                if(journal.Contains(target.Id))continue;
                SpriteRenderer seen=null;
                foreach(var r in target.Renderers)if(r!=null&&r.enabled&&r.gameObject.activeInHierarchy&&r.sprite!=null&&lens.Covers(selection.TargetCamera,r.bounds.center)){seen=r;break;}
                if(Hold(target.Id).Advance(seen!=null,Time.deltaTime,xraySeconds))Discover(target.Id,seen.bounds.center,null,seen,null,null);
            }
            if(bone!=null&&bone.HasFracture&&!journal.Contains(bone.ClueSymptomId))
            {
                bool visible=false;Vector3 point=default;var r=bone.ClueRenderer;
                if(r!=null&&r.enabled&&r.gameObject.activeInHierarchy)foreach(var uv in cracks){point=BoneClueMask.WorldPoint(r,uv);if(lens.Covers(selection.TargetCamera,point)){visible=true;break;}}
                if(Hold(bone.ClueSymptomId).Advance(visible,Time.deltaTime,xraySeconds))Discover(bone.ClueSymptomId,point,null,r,null,null);
            }
        }
        private void ObserveDialogue()
        {
            if(catalog==null||dialogue==null||!dialogue.TryGetClueText(out var text))return;
            // The words change on Next, but their visible-character limit changes every frame.
            // Compile candidate spans once per line, then only check how much has been revealed.
            if (matchedDialogue != text.text)
            {
                matchedDialogue = text.text;
                dialogueMatches.Clear();
                foreach (var match in catalog.DialogueMatches(matchedDialogue))
                    if (present.Contains(match.entry.symptomId))
                        dialogueMatches.Add((match.entry.symptomId, match.start, match.length, match.caption));
            }
            var info=text.textInfo;int count=Mathf.Min(text.maxVisibleCharacters,info.characterCount);int end=0;
            if(count>0){var last=info.characterInfo[count-1];end=last.index+last.stringLength;}
            foreach(var match in dialogueMatches)
            {
                if(journal.Contains(match.id)||match.start+match.length>end)continue;
                Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);bool visible=false;
                for(int i=0;i<count;i++){var ch=info.characterInfo[i];if(ch.index<match.start||ch.index>=match.start+match.length||!ch.isVisible)continue;min=Vector3.Min(min,text.transform.TransformPoint(ch.bottomLeft));max=Vector3.Max(max,text.transform.TransformPoint(ch.topRight));visible=true;}
                if(visible)Discover(match.id,(min+max)*.5f,null,null,match.caption,text.font);
            }
        }
        internal void Measurement(PatientExaminationProfile source,ClueMeasurement kind,Vector3 position)
        {
            SyncPatient();if(source!=patient||!CanObserve||catalog==null)return;
            foreach(var entry in catalog.Entries)if(entry.measurement==kind&&present.Contains(entry.symptomId))Discover(entry.symptomId,position,kind==ClueMeasurement.Temperature?thermometerIcon:heartIcon,null,null,null);
        }
        internal float HeartbeatSeconds=>heartbeatSeconds;
        private void Discover(string id,Vector3 world,Sprite icon,SpriteRenderer visual,string caption,TMP_FontAsset font)
        {
            if(!present.Contains(id)||!journal.Discover(patient.ConsultationId,id))return;
            Changed?.Invoke();
            if(flightPrefab==null||feedbackRoot==null||book==null)return;
            var capture=visual!=null&&spriteCapture!=null?spriteCapture.Capture(visual):null;
            var flight=Instantiate(flightPrefab,feedbackRoot,false);
            flight.Begin(selection.TargetCamera.WorldToScreenPoint(world),()=>selection.TargetCamera.WorldToScreenPoint(book.bounds.center),icon,capture,caption,font);
        }
        private void OnApplicationPause(bool value){paused=value;if(value)ResetXrayHolds();}
        private void OnApplicationFocus(bool value){unfocused=!value;if(!value)ResetXrayHolds();}
        private void OnDisable(){ResetNotice();if(feedbackRoot!=null)foreach(var flight in feedbackRoot.GetComponentsInChildren<ClueFlight>(true))Destroy(flight.gameObject);}
    }
}
