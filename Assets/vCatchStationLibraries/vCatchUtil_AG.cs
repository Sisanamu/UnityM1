/****************************************************************************
*                                                                           *
* vCatchUtil_AG.cs                                                          *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace vCatchStation
{
    public class vCatchUtil_AG
    {
        public vCatchUtil_AG()
        {
            _oq = Quaternion.identity;
        }
        public vCatchUtil_AG(Quaternion oq)
        {
            _oq = oq;
            _msec = 0;
        }

        float ms2Gravity = 9.80665f;

        public bool _init = false;
        Vector3 _gravity;
        Vector3 _a;
        uint _msec;

        Vector3 _vCur = new Vector3(0, 0, 0);

        public Vector3 _s = new Vector3(0, 0, 0);

        Quaternion _oq;

        public void ApplyData(float ax, float ay, float az, float gx, float gy, float gz, ushort dmsec)
        {
            az *= -1.0f;
            gz *= -1.0f;
            _msec += dmsec;

            // 중력가속도 추정
            float mA = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
            float coef = (mA > 1.05f || mA < 0.95f) ? 1000.0f : 10.0f; //@@

            const float iir = 0.9f; //@@ 지속적 변화에 적응
            _s.x = _s.x * iir + gx * (1.0f - iir);
            _s.y = _s.y * iir + gy * (1.0f - iir);
            _s.z = _s.z * iir + gz * (1.0f - iir);

            if (dmsec > 0) // frame속도에 관계없이 동작을 위해...
            {
                float x = dmsec / 10.0f;
                if (x < 1) x = 1;
                float y = (3 - 1.414f) / x + (2 * 1.414f - 2) / x / x;
                coef *= y;
            }
            if (!_init)
            {
                _gravity = _a = new Vector3(ax, ay, az);
                _s = new Vector3(0, 0, 0);
                _vCur = new Vector3(0, 0, 0);
                _oq = Quaternion.identity;
                _init = true;
            }
            else
            {
                _gravity = Quaternion.Euler(-gx, -gy, -gz) * _gravity;
                //_gravity = Quaternion.Euler(-_s.x, -_s.y, -_s.z) * _gravity;

                _a = new Vector3(ax, ay, az);

                _gravity.x = (_gravity.x * coef + ax) / (coef + 1);
                _gravity.y = (_gravity.y * coef + ay) / (coef + 1);
                _gravity.z = (_gravity.z * coef + az) / (coef + 1);
            }

            // move object
            //_oq = _oq * Quaternion.Euler(gx, gy, gz);
            _oq = _oq * Quaternion.Euler(gx, gy, gz);
            //_oq *= Quaternion.Euler(_s.x, _s.y, _s.z);

            // 속도계산 및 필터링
            _vCur += accelerationObject() * dmsec / 1000.0f;
            const float vCut = 0.1f; //@@
            if (_vCur.x > vCut) _vCur.x -= vCut;
            else if (_vCur.x < -vCut) _vCur.x += vCut;
            else _vCur.x = 0.0f;
            if (_vCur.y > vCut) _vCur.y -= vCut;
            else if (_vCur.y < -vCut) _vCur.y += vCut;
            else _vCur.y = 0.0f;
            if (_vCur.z > vCut) _vCur.z -= vCut;
            else if (_vCur.z < -vCut) _vCur.z += vCut;
            else _vCur.z = 0.0f;
        }

        public void GravityTo(Vector3 to)
        {
            // 중력을 아래로 회전
            Vector3 posg = _oq * _gravity;
            Quaternion qg = Quaternion.FromToRotation(posg, to);
            _oq = qg * _oq;
        }

        public void ZTo(Vector3 to, float percent = 1.0f)
        {
            Quaternion qg = Quaternion.FromToRotation(_gravity, new Vector3(0, -1, 0));
            Vector3 voz = qg * (new Vector3(0, 0, 1));
            float af = Mathf.Atan2(voz.x, voz.z);

            Vector3 posg = _oq * _gravity;
            //Vector3 vz = Quaternion.Inverse(_oq) * to;
            float at = Mathf.Atan2(to.x, to.z);
            //Trace.WriteLine(voz + " " + af + " " + vz + " " + at + " " + (at-af));

            Quaternion q = Quaternion.AngleAxis((af - at) / 3.14f * 180, posg);
            _oq = q * _oq;
        }

        public uint msec()
        {
            return _msec;
        }

        public Vector3 gravityLocal() // G 단위
        {
            return _gravity;
        }
        public Vector3 gravity() // G 단위
        {
            return _oq * _gravity;
        }

        public Vector3 accelerationLocal() // G 단위
        {
            return _a;
        }
        public Vector3 acceleration() // G 단위
        {
            return _oq * _a;
        }
        public Vector3 accelerationObject() // m/s2 단위
        {
            return _oq * (_a - _gravity) * ms2Gravity; //중력가속도 곱함
        }

        public Vector3 velocityLocal()
        {
            return _vCur;
        }

        public Quaternion rotation()
        {
            return _oq;
        }
    }
}
