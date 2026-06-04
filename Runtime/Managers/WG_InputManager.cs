
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WG_Casino
{
    /// <summary>
    /// * Instead of mapping the KeyCodes in many scripts, we just ask this script
    /// * to tell if that keyCode is pressed
    /// * Just call:
    /// 
    /// UGT_InputManager.IsConfirmInputPressedDown();
    /// </summary>
    public class WG_InputManager : MonoBehaviour
    {
        public static WG_InputManager Instance;

        [SerializeField] private KeyCode[] m_mainButtonKeyCodes = { KeyCode.Return, KeyCode.Joystick1Button0, KeyCode.Space, KeyCode.C };
        [SerializeField] private KeyCode[] m_addCoinKeyCodes = { KeyCode.UpArrow};
        [SerializeField] private KeyCode[] m_wagerKeyCodes = { KeyCode.DownArrow, KeyCode.Joystick1Button2 };
        [SerializeField] private KeyCode[] m_cancelkeyCodes = { KeyCode.Escape, KeyCode.Joystick1Button1 };
        [SerializeField] private KeyCode[] m_checkoutKeyCodes = { KeyCode.LeftArrow };

        private void Awake()
        {
            Instance = this;
        }
        /// <summary>
        /// Main Button: Interact, confirm...
        /// </summary>
        /// <returns></returns>
        bool _IsConfirmInputPressedDown(bool _down = false)
        {
            if (_down)
            {
                foreach (KeyCode kc in m_mainButtonKeyCodes)
                {
                    if (Input.GetKeyDown(kc))
                    {
                        Debug.Log("Click");
                        return true;
                    }
                }
            }
            else
            {
                foreach (KeyCode kc in m_mainButtonKeyCodes)
                {
                    if (Input.GetKey(kc))
                        return true;
                }
            }
            return false;
        }

        bool _IsAddCoinInputPressedDown(bool _down = false)
        {
            if (_down)
            {
                foreach (KeyCode kc in m_addCoinKeyCodes)
                {
                    if (Input.GetKeyDown(kc))
                    {
                        Debug.Log("Click");
                        return true;
                    }
                }
            }
            else
            {
                foreach (KeyCode kc in m_addCoinKeyCodes)
                {
                    if (Input.GetKey(kc))
                        return true;
                }
            }
            return false;
        }

        bool _IsWagerInputPressedDown(bool _down = false)
        {
            if (_down)
            {
                foreach (KeyCode kc in m_wagerKeyCodes)
                {
                    if (Input.GetKeyDown(kc))
                        return true;
                }
            }
            else
            {
                foreach (KeyCode kc in m_wagerKeyCodes)
                {
                    if (Input.GetKey(kc))
                        return true;
                }
            }
            return false;
        }

        bool _IsCheckouInputPressedDown(bool _down = false)
        {
            if (_down)
            {
                foreach (KeyCode kc in m_checkoutKeyCodes)
                {
                    if (Input.GetKeyDown(kc))
                        return true;
                }
            }
            else
            {
                foreach (KeyCode kc in m_checkoutKeyCodes)
                {
                    if (Input.GetKey(kc))
                        return true;
                }
            }
            return false;
        }
        bool _IsCancelInputPressedDown(bool _down = false)
        {
            if (_down)
            {
                foreach (KeyCode kc in m_cancelkeyCodes)
                {
                    if (Input.GetKeyDown(kc))
                        return true;
                }
            }
            else
            {
                foreach (KeyCode kc in m_cancelkeyCodes)
                {
                    if (Input.GetKey(kc))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Any code
        /// </summary>
        /// <param name="kc"></param>
        /// <returns></returns>
        public static bool IsKeyCodePressed(KeyCode kc)
        {
            return Input.GetKey(kc);
        }

        public static bool IsKeyCodePressedDown(KeyCode kc)
        {
            return Input.GetKeyDown(kc);
        }

        /// <summary>
        /// We just set a confirm keys but any keys can be set
        /// </summary>
        /// <returns></returns>
        public static bool IsConfirmInputPressedDown(bool _down = true)
        {
            return Instance._IsConfirmInputPressedDown(_down);
        }
        public static bool IsAddCoinInputPressedDown(bool _down = true)
        {
            return Instance._IsAddCoinInputPressedDown(_down);
        }
        public static bool IsWagerInputPressed(bool _down = true)
        {
            return Instance._IsWagerInputPressedDown(_down);
        }
        public static bool IsCheckouInputPressed(bool _down = true)
        {
            return Instance._IsCheckouInputPressedDown(_down);
        }
        public static bool IsCancelInputPressed(bool _down = true)
        {
            return Instance._IsCancelInputPressedDown(_down);
        }

    }
}
