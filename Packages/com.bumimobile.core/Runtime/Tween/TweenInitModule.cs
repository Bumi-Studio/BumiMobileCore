#pragma warning disable 0649

using UnityEngine;

namespace BumiMobile
{
    [RegisterModule("Tween", core: true, order: 800)]
    public class TweenInitModule : InitModule
    {
        public override string ModuleName => "Tween";

        [SerializeField] CustomEasingFunction[] customEasingFunctions;

        [Space]
        [SerializeField] int tweensUpdateCount = 300;
        [SerializeField] int tweensFixedUpdateCount = 30;
        [SerializeField] int tweensLateUpdateCount = 0;

        [Space]
        [SerializeField] bool verboseLogging;

        public override void CreateComponent()
        {
            if (InitializerContext.GameObject == null)
            {
                Debug.LogError("[TweenInitModule] InitializerContext is not ready. Ensure Initializer has executed before creating Tween module.");
                return;
            }

            Tween tween = InitializerContext.GameObject.AddComponent<Tween>();
            tween.Init(tweensUpdateCount, tweensFixedUpdateCount, tweensLateUpdateCount, verboseLogging);

            Ease.Init(customEasingFunctions);
        }
    }
}
