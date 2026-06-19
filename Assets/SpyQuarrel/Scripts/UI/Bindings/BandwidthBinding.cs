using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    public class BandwidthBinding : CustomBinding
    {
    
        public BandwidthBinding()
        {
            this.updateTrigger = BindingUpdateTrigger.EveryUpdate;
        }

        protected override BindingResult Update(in BindingContext context)
        {
            // string value = "unset";
            // UnityTransport transport;
            // VisualElement targetElement = context.targetElement;
            // if (targetElement == null) return new BindingResult(BindingStatus.Failure);
            // if (!Application.isPlaying) return new BindingResult(BindingStatus.Failure);
            // if(NetworkManager.Singleton == null) return new BindingResult(BindingStatus.Failure);
            // transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();
            // if (transport == null)
            // {
            //     transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            //     if (transport == null) return new BindingResult(BindingStatus.Failure);
            // }
            // if(transport == null) return new BindingResult(BindingStatus.Failure);
            //
            // value = transport.
            //
            //
            // if(ConverterGroups.TrySetValueGlobal(ref targetElement, context.bindingId, ))
            //
            //
            return base.Update(in context);
        }
    }
}
