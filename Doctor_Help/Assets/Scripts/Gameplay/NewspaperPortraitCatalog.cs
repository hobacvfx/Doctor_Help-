using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(menuName = "Doctor Help/UI/Newspaper Portraits")]
    public sealed class NewspaperPortraitCatalog : ScriptableObject
    {
        [Serializable] public sealed class Portrait
        {
            public string npcId;
            public Sprite image;
        }
        [SerializeField] private NpcProfile source;
        [SerializeField] private List<Portrait> portraits = new List<Portrait>();
        public NpcProfile Source => source;
        public Sprite Find(string npcId) => portraits.Find(x => x != null && x.npcId == npcId)?.image;
    }
}
