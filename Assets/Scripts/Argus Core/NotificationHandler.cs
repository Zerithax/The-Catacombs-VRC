/*
//using SAO.Statics;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRRefAssist;
using AudioManager = Argus.Audio.AudioManager;

namespace Argus.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [Singleton]
    public class NotificationHandler : UdonSharpBehaviour
    {
        #region SINGLETONS
        [SerializeField] [HideInInspector] private AudioManager audioManager;
        [SerializeField] [HideInInspector] private ItemManager itemManager;
        [SerializeField] [HideInInspector] private PlayerProportions playerProportions;
        [SerializeField] public PlayerAttributesHandler playerAttributesHandler;
        #endregion

        [SerializeField] private LayerMask enviroMask;
        [SerializeField] private GameObject hexWarningPrefab;
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private GameObject textFieldPrefab;
        [SerializeField] private GameObject acknowledgePrefab;
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private GameObject notificationBigPrefab;
        [SerializeField] private GameObject notificationProgressPrefab;
        [SerializeField] private GameObject levelUpPrefab;
        [SerializeField] private GameObject virtualItemInspectorPrefab;
        [SerializeField] private GameObject upgradeableItemInspectorPrefab;
        [SerializeField] private GameObject resultPrefab;
        [SerializeField] private GameObject progressionWindowPrefab;
        [SerializeField] private GameObject tradeWindowPrefab;
        [SerializeField] private GameObject playerModerationWindowPrefab;
        [SerializeField] private GameObject itemSelectionWindowPrefab;
        [SerializeField] private GameObject marketItemPreviewPrefab;
        [SerializeField] private GameObject marketPurchaseConfirmPrefab;
        [SerializeField] private GameObject questWindowPrefab;

        [Header("Colors")]
        [SerializeField] private Color errorHexCol;
        [SerializeField] private Color metawarningHexCol;
        [SerializeField] private Color warningHexCol;

        /// <summary>
        /// Spawns an SAO hex style notification in front of the player
        /// </summary>
        /// <param name="hexType">Type of the hex</param>
        /// <param name="noteText">text to send to the player</param>
        /// <param name="openTime">time the warning message stays open</param>
        public HexWarning _SetupHexWarning(HexType hexType, string noteText, float openTime = 0)
        {
            GameObject newHexObject = Instantiate(hexWarningPrefab);
            
            HexWarning hexWarnComp = newHexObject.GetComponent<HexWarning>();

            Color hexCol;
            switch (hexType)
            {
                default:
                case HexType.Error:
                    hexCol = errorHexCol;
                    break;
                case HexType.MetaWarning:
                    hexCol = metawarningHexCol;
                    break;
                case HexType.Warning:
                    hexCol = warningHexCol;
                    break;
            }

            hexWarnComp.Initialize(noteText, hexCol, openTime);

            _SetupNotificationPosition(newHexObject.transform);

            return hexWarnComp;
        }

        public HexWarning _SetupHexWarningWorld(HexType hexType, string noteText, float openTime, Vector3 position, Quaternion rotation)
        {
            var hex = _SetupHexWarning(hexType, noteText, openTime);
            
            hex.SetMovementBehaviour(MovementBehaviour.WorldLocked);
            
            hex.transform.position = position;
            hex.transform.rotation = rotation;
            return hex;
        }

        public void _SetupDebugHexWarning(string contents)
        {
            _SetupHexWarning(HexType.MetaWarning, $"<b>Debug Message</b>\n{contents}", 3);
        }

        /// <summary>
        /// Spawns an SAO style notification in front of the player
        /// </summary>
        /// <param name="headerText">Header text</param>
        /// <param name="bodyText">Body text</param>
        /// <param name="callbackBehaviour">Target callback behaviour</param>
        /// <param name="acceptEvent">Accept callback</param>
        /// <param name="rejectEvent">Reject callback</param>
        public Prompt _SetupPrompt(string headerText, string bodyText, UdonSharpBehaviour callbackBehaviour, string acceptEvent, string rejectEvent)
        {
            GameObject newPrompt = Instantiate(promptPrefab);

            Prompt prompt = newPrompt.GetComponent<Prompt>();

            prompt.Initialize(headerText, bodyText, callbackBehaviour, acceptEvent, rejectEvent);

            _SetupNotificationPosition(newPrompt.transform);

            return prompt;
        }

        public Acknowledge _SetupAcknowledge(string headerText, string bodyText, UdonSharpBehaviour callbackBehaviour, string callbackEvent)
        {
            GameObject newPrompt = Instantiate(acknowledgePrefab);

            Acknowledge acknowledge = newPrompt.GetComponent<Acknowledge>();

            acknowledge.Initialize(headerText, bodyText, callbackBehaviour, callbackEvent);

            _SetupNotificationPosition(newPrompt.transform);

            return acknowledge;
        }

        public VirtualItemInspector _SetupVirtualItemInspector(string headerText, string bodyText, VirtualItem item)
        {
            GameObject newPrompt = Instantiate(virtualItemInspectorPrefab);

            VirtualItemInspector inspector = newPrompt.GetComponent<VirtualItemInspector>();

            inspector.Initialize(headerText, bodyText, item);

            _SetupNotificationPosition(newPrompt.transform);

            return inspector;
        }

        public UpgradeableItemInspector _SetupUpgradeableItemInspector(string headerText, string bodyText,
            VirtualItem item)
        {
            GameObject newPrompt = Instantiate(upgradeableItemInspectorPrefab);

            UpgradeableItemInspector inspector = newPrompt.GetComponent<UpgradeableItemInspector>();

            inspector.Initialize(headerText, bodyText, item);

            _SetupNotificationPosition(newPrompt.transform);

            return inspector;
        }
        
        public ResultWindow _SetupResultWindow(int exp, int col, int items)
        {
            GameObject newPrompt = Instantiate(resultPrefab);

            ResultWindow prompt = newPrompt.GetComponent<ResultWindow>();

            prompt.Initialize(exp, col, items);

            return prompt;
        }

        public ResultWindow _SetupResultWindowWorld(int exp, int col, int items, Vector3 pos)
        {
            var result = _SetupResultWindow(exp, col, items);
            
            result.SetMovementBehaviour(MovementBehaviour.WorldFacingHead);
            
            result.transform.position = pos;
            return result;
        }
        
        public PlayerModerationWindow _SetupPlayerModerationWindow(VRCPlayerApi player)
        {
            GameObject moderationWindow = Instantiate(playerModerationWindowPrefab);

            PlayerModerationWindow prompt = moderationWindow.GetComponent<PlayerModerationWindow>();

            prompt.Initialize(player);
            
            prompt.SetMovementBehaviour(MovementBehaviour.WorldLocked);

            return prompt;
        }
        
        public ItemSelectionWindow _SetupItemSelectionWindow(string headerText, Vector2Int[] source, UdonSharpBehaviour callbackBehaviour, string callbackEvent, string cancelEvent = "")
        {
            GameObject newPrompt = Instantiate(itemSelectionWindowPrefab);

            ItemSelectionWindow prompt = newPrompt.GetComponent<ItemSelectionWindow>();

            prompt.Initialize(headerText, source, callbackBehaviour, callbackEvent, cancelEvent);

            _SetupNotificationPosition(newPrompt.transform);

            return prompt;
        }
        
        public MarketItemPreview _SetupItemBuyPreviewWindow(int itemId, UdonSharpBehaviour callbackBehaviour, string callbackEvent, string cancelEvent = "")
        {
            GameObject newPrompt = Instantiate(marketItemPreviewPrefab);

            MarketItemPreview prompt = newPrompt.GetComponent<MarketItemPreview>();

            prompt.Initialize(itemId, callbackBehaviour, callbackEvent, cancelEvent);

            _SetupNotificationPosition(newPrompt.transform);

            return prompt;
        }
        
        public MarketConfirmPopup _SetupMarketConfirmPopup(int itemId, int amount)
        {
            GameObject popup = Instantiate(marketPurchaseConfirmPrefab);
            
            MarketConfirmPopup confirmPopup = popup.GetComponent<MarketConfirmPopup>();

            confirmPopup.Initialize(itemId, amount);
            
            _SetupNotificationPosition(popup.transform);
            
            return confirmPopup;
        }

        /// <summary>
        /// Spawns an SAO style notification in front of the player suitable for text entry
        /// </summary>
        /// <param name="headerText">Header text</param>
        /// <param name="bodyText">Body text</param>
        /// <param name="callbackBehaviour">Target callback behaviour</param>
        /// <param name="acceptEvent">Accept callback</param>
        /// <param name="rejectEvent">Reject callback</param>
        public TextFieldNotification _SetupTextFieldNotification(string headerText, string bodyText, int maxChars,
            UdonSharpBehaviour callbackBehaviour, string acceptEvent, string rejectEvent)
        {
            GameObject newNotification = Instantiate(textFieldPrefab);

            TextFieldNotification textFieldNotification = newNotification.GetComponent<TextFieldNotification>();

            textFieldNotification.Initialize(headerText, bodyText, maxChars,callbackBehaviour, acceptEvent, rejectEvent);

            _SetupNotificationPosition(newNotification.transform);

            return textFieldNotification;
        }
        
        public ProgressNotification _SetupProgressNotification(string headerText, string bodyText, string progressName, float progress)
        {
            GameObject newNotification = Instantiate(notificationProgressPrefab);

            ProgressNotification progressNotification = newNotification.GetComponent<ProgressNotification>();

            progressNotification.Initialize(headerText, bodyText);
            
            progressNotification._SetProgress(progress);
            progressNotification._SetProgressText(progressName);

            _SetupNotificationPosition(progressNotification.transform);

            return progressNotification;
        }

        public Notification _SetupNotification(string headerText, string bodyText, float timeoutPeriod)
        {
            return _SetupBaseNotification(headerText, bodyText, timeoutPeriod, notificationPrefab);
        }

        public Notification _SetupNotificationBig(string headerText, string bodyText, float timeoutPeriod)
        {
            return _SetupBaseNotification(headerText, bodyText, timeoutPeriod, notificationBigPrefab);
        }

        public LevelUp _SetupLevelNotification(int newLevel, string playerName, float timeoutPeriod)
        {
            GameObject newWindow = Instantiate(levelUpPrefab);
            
            LevelUp levelUp = newWindow.GetComponent<LevelUp>();
            
            levelUp.Initialize(playerName, newLevel-1, newLevel, timeoutPeriod);
            
            _SetupNotificationPosition(newWindow.transform);

            return levelUp;
        }
        
        public void _OpenProgressionWindow()
        {
            _SetupProgressionWindow(playerAttributesHandler.Agility, playerAttributesHandler.Strength);
        }

        public ProgressionWindow _SetupProgressionWindow(int currentAgility, int currentStrength)
        {
            Debug.Log($"Running _SetupProgressionWindow {currentAgility} {currentStrength}");
            
            GameObject newWindow = Instantiate(progressionWindowPrefab);
            
            ProgressionWindow progressionWindow = newWindow.GetComponent<ProgressionWindow>();
            
            progressionWindow.Initialize(currentAgility, currentStrength);
            
            _SetupNotificationPosition(newWindow.transform);
            
            progressionWindow.SetMovementBehaviour(MovementBehaviour.LerpToView);
            
            return progressionWindow;
        }

        public TradeWindow _SetupTradeWindow(VRCPlayerApi tradingPartner, TradeManager tradeManager, bool isInitiator)
        {
            GameObject newWindow = Instantiate(tradeWindowPrefab);

            TradeWindow tradeWindow = newWindow.GetComponent<TradeWindow>();

            tradeWindow.Initialize(tradingPartner, tradeManager, isInitiator);

            _SetupNotificationPosition(newWindow.transform);

            return tradeWindow;
        }
        
        public QuestWindow _SetupQuestWindow(Quest quest)
        {
            GameObject newWindow = Instantiate(questWindowPrefab);

            QuestWindow questWindow = newWindow.GetComponent<QuestWindow>();

            questWindow.Initialize(quest);

            return questWindow;
        }

        private float baseMaxDistance = 0.4f;
        private float maxDistanceScaled => baseMaxDistance * playerProportions.LocalPlayerScale;

        public void _SetupNotificationPosition(Transform notificationObj)
        {
            VRCPlayerApi.TrackingData head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            Vector3 headDirection = head.rotation * Vector3.forward * maxDistanceScaled;

            Vector3 posToSpawn = head.position + headDirection;

            RaycastHit hitObject;
            if (Physics.Raycast(head.position, headDirection, out hitObject, maxDistanceScaled, enviroMask))
            {
                posToSpawn = hitObject.point - headDirection * 0.15f;
            }

            Quaternion newRot = Quaternion.LookRotation(posToSpawn - head.position);

            notificationObj.SetPositionAndRotation(posToSpawn, newRot);
        }

        private Notification _SetupBaseNotification(string headerText, string bodyText, float timeoutPeriod, GameObject prefab)
        {
            GameObject newNotification = Instantiate(prefab);

            Notification notification = newNotification.GetComponent<Notification>();

            notification.Initialize(headerText, bodyText, timeoutPeriod);

            _SetupNotificationPosition(notification.transform);

            return notification;
        }
    }
}
*/