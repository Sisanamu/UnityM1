namespace vCatchStation
{
    public class vCatchStation
    {
        // 3.21.2

        ///////////////////////////////////////////////////////////////////////////////
        // Interface between Middleware and Application(Game)

        public const int App_TCP_PortNumber = 0x3703;

        // Initialize vCatch
        // Application(Game)에서 Middleware로 사용할 항목을 예약하고 초기화
        //    {
        //        "init":[
        //            {
        //                "type":protocol-type,               // (string) - 시작할 통신형식. report/screen/click/drag
        //                "sensitivity":sensitivity,          // (string) optional - 최소감지능력 : a:BB탄(300km/h) b:야구공고속(100km/h) c:야구공저속(50km/h)(default) d:배구공고속(50km/h) e:배구공저속(20km/h) f:손터치
        //                "face":face-name,                   // (string) optional - 동작면 : W1(default 벽1),W2(벽2),F1(바닥1)...
        //                "required":[sensor-name, ...],      // (string or string array) optional - 필수로 존재해야 할 센서명(들). 센서 선택의 선호도나 센서를 사용하는 것과는 관련 없음
        //                "preferred":[sensor-name, ...],     // (string or string array) optional - 선호하는 센서명(들).
        //                "weakly":[true/false]               // (boolean) optional - 다른앱이 점유하고 있으면 실패처리 default:false
        //            }
        //            [, 반복]                                // optional
        //        ]
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {init:[{type:"click"}]} - click을 감지하는 센서 초기화
        //    {init:[{type:"touch",preferred:["A"]}]} - touch-down/move/up을 감지하는 센서 중 A센서가 있으면 우선적으로 적용하고 초기화
        //    {init:[{type:"click",required:["A"]}]} - click을 감지하는 센서 중 A센서가 꼭 있어야하고 초기화
        //    {init:[{type:"click"},{type:"drag"}]} - click을 감지하는 센서와 touch-down/move/up을 감지하는 센서 초기화
        //    {init:[{type:"screen",face:"W1"},{type:"click",face:"W1",preferred:["A","B"],required:["a"]}]} - 
        //    {init:[{type:"screen",face:"W1"},{type:"screen",face:"W2"},{type:"click",face:"W1"]} - 
        //
        // Return
        //    {
        //        protocol-type:{                             // (string) - sceen, click, drag...
        //            face-name:{
        //                // protocol-type이 screen인 경우
        //                "left":left-position,
        //                "top":top-position,
        //                "width":width-size,
        //                "height":height-size
        //                // screen에 대한 dimention 값이 존재하면
        //                "mm-width":physical-width,          // (integer) optional
        //                "mm-height":physical-height,        // (integer) optional
        //                "mm-bottom":physical-bottom-height  // (integer) optional 스크린하단의 바닥기준 y 위치(mm). 스크린이 바닥에서 위로 띄워져있으면 양수
        //                // 센서인 경우
        //                "sensor":sensor-name,
        //                "revision":protocol-version,        // (integer) - protocol version
        //                "sensitivity":sensitivity,          // (string) - 지원하는 최대 감지능력
        //                "extra":{extra-information}         // (object) - exrta-information of sensor-driver
        //            }
        //            [, 반복]                                // extra - 지원하는 화면
        //        }
        //        [, 반복]                                    // extra - 지원하는 프로토콜
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {"screen":{"W1":{"left":0,"top":0,"width":100,"height":100}},"click":{"W1":{"sensor":"T3k","revision":1}}}

        // Do vCatch
        // Application(Game)에서 Middleware로 예약한 항목의 사용을 요청
        //    {
        //        "do":protocol-type|face-name,               // (string) - 대상 통신형식
        //        extra ...                                   // optional - 부가 정보
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {do:"click|W1"} - 2차원좌표를 감지시작
        //    {do:"drag|W2",timeout:1000} - 2차원좌표를 감지시작, 1초후 감지종료
        //    {do:"drag|W1",cmd:"stop"} - 2차원좌표를 감지종료
        //
        // Return
        //    {
        //        "data":{                                    // (object) - 센싱 데이터
        //            protocol-type|face-name:[               // (string) - 대상 통신형식
        //                data                                // (object) - protocol-type에 따른 데이터, 비어있으면 종료로 알림
        //                [, 반복]                            // extra - 데이터 추가
        //            ]
        //            [, 반복]                                // extra - 통신형식 추가
        //        },
        //        "state":{                                   // extra - 센서상태
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {"data":{"click|W1":[{"x":0.3,"y":0.2}]}}

        // Deinitialize vCatch
        // Application(Game)에서 Middleware로 사용한 항목들의 사용예약을 반환
        //    {
        //        "deinit"
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {"deinit":[]}
        //
        // Return
        //    Socket Closing

        // Extra function - Data
        // report protocol-type을 사용하는 미들웨어에 데이터를 발생
        //    {
        //        "data":{                                    // (object) - 센싱 데이터
        //            protocol-type|face-name:{               // (string) - 대상 통신형식
        //                data                                // (object) - protocol-type에 따른 데이터
        //            }
        //            [, 반복]                                // extra - 통신형식 추가
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        //
        // Return
        //    없음


        ///////////////////////////////////////////////////////////////////////////////
        // Interface to Middleware

        // Logging
        // Middleware에 Log message를 전달함
        //    {
        //        "log":{
        //            "time":system-time,                     // (integer)
        //            "level":level,                          // (integer) - information, warnning, debuging
        //            "tag":tag,                              // (string) - vStation, vCatch, vDriver, etc...
        //            "msg":message,                          // (string)
        //            "file":code-filename,                   // (string)
        //            "line":line number in code-filename,    // (integer)
        //            "skipped":count of skipped message      // (integer)
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {log:{time:123456,level:1,tag:"vCatch,msg:"Error",file:"abc.c",line:100,skipped:0}} - 센서 초기화
        //
        // Return
        //    없음

        public const int TraceLevel_Information = 1;
        public const int TraceLevel_Warnning = 2;
        public const int TraceLevel_Debug = 6;

        // Echo
        // Middleware에 전달하여 자신이 받을 재귀패킷
        //    {
        //        "echo":echo-packet
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {echo:{echo:{}} - 자신이 echo를 받는다
        //
        // Return
        //    echo-packet


        ///////////////////////////////////////////////////////////////////////////////
        // Interface between Middleware and Sensor-driver

        public const int Driver_TCP_PortNumber = 0x3603;

        // Initialize vCatchDriver
        // Sensor-driver에서 Middleware로 ProcessId 전달
        //    4bytes little-endian ProcessId
        // * ProcessId가 0이면 Driver가 아닌 Log나 Screen정보를 주고 받는 연결
        //
        // Middleware에서 Sensor-driver로 등록됨을 통보
        //    {
        //        "init":{
        //            "ini":ini-filepath,                     // (string) - 설정(ini)파일 경로
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {init:{ini:"c:\vCatchStation\aaa.ini"}} - 센서 초기화
        //
        // Return
        //    {
        //        "init":{
        //            "sensor":sensor-name,
        //            "version":sensor-version,               // (string) - 센서 버젼
        //            "detail":sensor-description,            // (string) optional - 센서 상세 정보
        //            "capability"{
        //                protocol-type:{
        //                    "revision":protocol-version,    // (integer) - 프로토콜 버젼
        //                    "sensitivity":sensitivity       // (string) - 지원하는 최대 감지능력. a:BB탄(300km/h) b:야구공고속(100km/h) c:야구공저속(50km/h) d:배구공고속(50km/h) e:배구공저속(20km/h) f:손터치
        //                    "extra":{                       // (object) - exrta-information of sensor-driver
        //                        "mm-hp-screen":distance.    // (integer) optional(비젼센서경우) : HittingPoint와 스크린간 거리(mm)
        //                        "mm-hp-ground":height       // (integer) optional(비젼센서경우) : 바닥기준 HittingPoint의 y 위치(mm). 스크린이 바닥에서 위로 띄워져있으면 양수
        //                    }
        //                }
        //                [, 반복]                            // extra - 지원하는 프로토콜
        //            },
        //            "error":error-message                   // (string) optional - 초기화 중 Error가 발생하면 메세지 전달
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {"init":{"sensor":"T3k","version":"1.0","capability":{"click":{"revision":1,"sensitivity":"c"}}}}

        // Wakeup vCatchDriver
        // Middleware에서 Sensor-driver로 통보
        //    {
        //        "wakeup":{
        //            ...  // init
        //            "required":[sensor-name, ...],      // (string or string array) optional - 필수로 존재해야 할 센서명(들). 센서 선택의 선호도나 센서를 사용하는 것과는 관련 없음
        //            "preferred":[sensor-name, ...]      // (string or string array) optional - 선호하는 센서명(들).
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {"wakeup":{}} - wakeup 센서
        //
        // Return - 없음

        // Do vCatchDriver
        // Middleware에서 Sensor-driver로 프로토콜에 해당하는 센싱 요청
        //    {
        //        "do":protocol-type|face-name,               // (string) - 대상 통신형식
        //        "cmd":command,                              // (string) optional - 실행할 명령
        //        "screen":{                                  // (object) extra - face 정보. 변경시 독립적으로도 전송 가능
        //            face-name:{
        //                "left":left-position,
        //                "top":top-position,
        //                "width":width-size,
        //                "height":height-size
        //            }
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {do:"click|W1"} - 2차원좌표를 감지시작
        //    {do:"drag|W2",timeout:1000} - 2차원좌표를 감지시작, 1초후 감지종료
        //    {do:"drag|W1",cmd:"stop"} - 2차원좌표를 감지종료
        //
        // Return
        //    {
        //        "data":{                                    // (object) - 센싱 데이터
        //            protocol-type|face-name:[               // (string) - 대상 통신형식
        //                data                                // (object) - protocol-type에 따른 데이터, 비어있으면 종료로 알림
        //                [, 반복]                            // extra - 데이터 추가
        //            ]
        //            [, 반복]                                // extra - 통신형식 추가
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {"data":{"click|W1":[{"x":100,"y":200}]}}

        // Sleep vCatchDriver
        // Middleware에서 Sensor-driver로 통보
        //    {
        //        "sleep":{
        //        }
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {"sleep":{}} - sleep 센서
        //
        // Return - 없음

        // Deinitialize vCatchDriver
        // Middleware에서 Sensor-driver로 종료를 통보함
        //    {
        //        "deinit"
        //    }
        //    \n                                              // packet의 종료 문자
        // Example :
        //    {deinit}
        //
        // Return
        //    Socket Closing
    }


    ///////////////////////////////////////////////////////////////////////////////
    // Data structure definition

    // report
    // Middleware에 장착된 센서 정보
    //    {
    //        screen:{                                     // (string) -
    //            face-name:{
    //                "left":left-position,
    //                "top":top-position,
    //                "width":width-size,
    //                "height":height-size
    //            }
    //            [, 반복]                                 // extra - 지원하는 화면
    //        },
    //        report:{                                     // (string) -
    //            face-name:{
    //                protocol-type:[                      // (string) - click, drag...
    //                    {
    //                        "name":sensor-name,          // (string) - sensor name
    //                        "version":sensor-version,    // (string) - sensor version
    //                        "revision":protocol-version, // (integer) - protocol version
    //                        "sensitivity":sensitivity,   // (string) - 지원하는 최대 감지능력
    //                        "ini-file":ini-filename,     // (string) - 설정파일명
    //                        "state":state                // (string) - 상태 (awake:앱에 할당됨, asleep:쉬고 있음) - middleware가 업데이트함
    //                    }
    //                    [, 반복]                         // extra - 설치된 sensor
    //                ]
    //                [, 반복]                             // extra - 지원하는 protocol
    //            }
    //            [, 반복]                                 // extra - 지원하는 화면
    //        }
    //    }
    // Example :
    //    {"screen":{"W1":{"left":0,"top":0,"width":640,"height":480}},"report":{"W1":{"click":[{"name":"vSensorEmulator","version":"1.0","revision":1,"sensitivity":"a","ini-file":"vSensorEmulator_W1.ini"}]}}}

    // screen
    // 화면 정보
    //    {
    //        "left":left-position,  // (integer)
    //        "top":top-position,    // (integer)
    //        "width":width-size,    // (integer)
    //        "height":height-size   // (integer)
    //        // setting 값이 존재하면
    //        "mm-width":physical-width,         // (integer) optional
    //        "mm-height":physical-height,       // (integer) optional
    //        "mm-bottom":physical-bottom-height // (integer) optional 스크린하단의 바닥기준 y 위치(mm). 스크린이 바닥에서 위로 띄워져있으면 양수
    //    }
    // Example :
    //    {"left":0,"top":0,"width":1024,"height":768}
    public class vScreen
    {
        public vScreen()
        {
            left = top = 0;
            width = height = 0;
            unityscreen = 0;
            mm_width = mm_height = mm_bottom = null;
        }
        public vScreen(int left, int top, int width, int height, int unityscreen,
                    int? mm_width, int? mm_height, int? mm_bottom)
        {
            this.left = left;
            this.top = top;
            this.width = width;
            this.height = height;
            this.unityscreen = unityscreen;
            this.mm_width = mm_width;
            this.mm_height = mm_height;
            this.mm_bottom = mm_bottom;
        }
        public int left;
        public int top;
        public int width;
        public int height;
        public int unityscreen;

        public int? mm_width;
        public int? mm_height;
        public int? mm_bottom;

        new public string ToString()
        {
            string ret = "vScreen:(" + left + "," + top + ")," + width + "x" + height + "[" + unityscreen + "]";
            if (mm_width != null)
                ret += " mm-width:" + mm_width;
            if (mm_height != null)
                ret += " mm-height:" + mm_height;
            if (mm_bottom != null)
                ret += " mm-bottom:" + mm_bottom;
            return ret;
        }
    }

    // click
    // 감지된 점 정보
    //    {
    //        "x":position-x,        // (float) - 0~1
    //        "y":position-y,        // (float) - 0~1
    //        "t":system-time        // (16bits integer) - 
    //    }
    // Example :
    //    {"x":0.123,"y":0.456,"t":1234567}
    public class vClick
    {
        public vClick()
        {
            x = y = 0.0F;
            time = 0;
        }
        public vClick(float x, float y, ushort time)
        {
            this.x = x;
            this.y = y;
            this.time = time;
        }
        public float x;
        public float y;
        public ushort time;
    }

    // drag
    // 감지된 물체의 크기/Drag 정보
    //    {
    //        "i":id,                // (integer) - id
    //        "s":status,            // (integer) - 0:move 1:down 2:up
    //        "x":position-x,        // (float) - 0~1
    //        "y":position-y,        // (float) - 0~1
    //        "r":object-radius,     // (float) - 포인트의 크기(반지름), screen width에 대한 비율
    //        "t":system-time        // (16bits integer) - 
    //    }
    // Example :
    //    {"i":0,"s":0,"x":0.123,"y":0.456,"r":0.05,"t":1234567}
    public class vDrag
    {
        public static int MOVE = 0;
        public static int DOWN = 1;
        public static int UP   = 2;

        public bool Down
        {
            get { return (status & DOWN) != 0; }
        }
        public bool Up
        {
            get { return (status & UP) != 0; }
        }
        public bool Move
        {
            get { return (status & (DOWN | UP)) == 0; }
        }

        public vDrag()
        {
            id = -1;
            status = MOVE;
            x = y = r = 0.0F;
            time = 0;
        }
        public vDrag(int id, int status, float x, float y, float r, ushort time)
        {
            this.id = id;
            this.status = status;
            this.x = x;
            this.y = y;
            this.r = r;
            this.time = time;
        }
        public int id;
        public int status;
        public float x;
        public float y;
        public float r;
        public ushort time;
    }

    // speedpoint
    // 감지된 물체의 위치와 속도 정보
    //    {
    //        "x":position-x,        // (float) - 0~1
    //        "y":position-y,        // (float) - 0~1
    //        "km_h":object-radius,  // (float) - km/h
    //        "t":system-time        // (16bits integer) - 
    //    }
    // Example :
    //    {"x":0.123,"y":0.456,"km_h":60.,"t":1234567}
    public class vSpeedPoint
    {
        public vSpeedPoint()
        {
            x = y = 0.0F;
            km_h = 0.0F;
            time = 0;
        }
        public vSpeedPoint(float x, float y, float km_h, ushort time)
        {
            this.x = x;
            this.y = y;
            this.km_h = km_h;
            this.time = time;
        }
        public float x;
        public float y;
        public float km_h;
        public ushort time;
    }

    // camera
    // 카메라
    // - do vCatch후 데이터는 1회 전달. 연결이 끊기면 사용중인 카메라를 중단해야 함.
    //    {
    //         "idx":camera-index,   // (integer) - 카메라 인덱스 번호
    //         "name":device-name,   // (string) - 장치이름
    //         "alias":device-alias, // (string) - 장치 alias. 장치이름이 중복되지 않도록 한 별칭
    //         "path":device-path,   // (string) - 장치 path
    //         "res":[
    //              {
    //                   "w":width,  // (integer) - 화면 폭
    //                   "h":height, // (integer) - 화면 높이
    //                   "f":format  // (string) - 포맷 YUY2
    //              }
    //              [, 반복]         // extra - media type
    //         ]
    //    }
    // Example
    //    {"idx"0,"path":"\\\\?\\usb#vid_0000&pid_0000&mi_00#0000#{00000000-0000-0000-0000-000000000000}\\global", "res" : [{"w":640, "h" : 480, "f" : "YUY2"}, { "w":1280,"h" : 720,"f" : "YUY2" }]
    public class vCamera
    {
        public class Resolution
        {
            public Resolution(int width, int height, string format)
            {
                this.width = width;
                this.height = height;
                this.format = format;
            }

            public int width;
            public int height;
            public string format;
        }

        public vCamera()
        {
            idx = -1;
        }
        public vCamera(int idx, string name, string alias, string path, Resolution[] resolutions)
        {
            this.idx = idx;
            this.name = name;
            this.alias = alias;
            this.path = path;
            this.resolutions = resolutions;
        }
        public int idx;
        public string name;
        public string alias;
        public string path;
        public Resolution[] resolutions;
    }

    // multimodal_b, multimodal_jb, multimodal_agb, multimodal_agjb, multimodal_p
    // 멀티모달센서 Acceleration xyz, Gyro xyz, (Joystick xy), Button states
    // do extra :
    //    "player-ids":[],           // (integer array) optional - [] 플레이어 선택 모드 (추가연결 허용), [id,id...] 사용할 플레이어 id 목록 (추가연결 막음)
    //    "cmd":{                    // (object) optional
    //        "pids":[...],          // (integer array) - 플레이어 id 목록
    //        "connect":string,      // (string) optional - silence(데이터 보내지 말것), data(데이터 요청), disconnect(연결 끊기)
    //        "rumble":string,       // (string) optional - "진동msec 무진동msec 반복횟수"
    //        "color":string         // (string) optional - 4bits(넘버) 4bits(red) 4bits(green) 4bits(blue), player-ids 인자가 있으면 초기화됨
    //    }
    // Example :
    //    "do":"...","player-ids":[],"cmd":{"pids":[1,2],"connect":"disconnect"} - 1,2번 플레이어 연결 끊고, 플레이어 선택 모드 시작
    //    "do":"...","player-ids":[1,2] - 플레이어 1,2로 시작
    // Data :
    //    {
    //        "i":id,                // (integer) - id > 0
    //        "a":[x,y,z],           // (float array) - acceleration xyz(G unit)
    //        "g":[x,y,z],           // (float array) - gyro xyz(radian unit)
    //        "dt":delta-time        // (16bits integer) dependent on acceleration xyz(milliseconds unit) - delta of system-time
    //        "j":[x,y],             // (float array) - joystick xy(-1~1) (multimodal_agjb 경우)
    //        "b":flags,             // (integer) - button state flags 0x01:S 0x02:L 0x08:X 0x10:J
    //        "t":system-time        // (16bits integer) - msec timestamp
    //    }
    //    // player information 경우
    //    {
    //        "i":0,                 // (integer) - 0:updated -1:removed
    //        "p":{                  // (object)
    //           "i":id,             // (integer) - 플레이어 id
    //           "parts":string,     // (string) - left/right gold/aqua/
    //           "batlevel":float,   // (float) optional - 0~1, -1:조회중
    //           "charging":boolean  // (boolean) optional - true/false
    //        }
    //    }
    // Example :
    //    {"i":1,"d":{...},"t":1234567} - 데이터
    //    {"i":0,"p":{"i":1,"s":"standby"},"t":1234567} - 플레이가 변경되었을 경우
    public class vMmPlayer
    {
        public vMmPlayer()
        {
            id = -1;
        }
        public vMmPlayer(int id, string parts, float? batlevel, bool? charging)
        {
            this.id = id;
            this.parts = parts;
            this.batlevel = batlevel;
            this.charging = charging;
        }
        public int id;
        public string parts;
        public float? batlevel;
        public bool? charging;
    }

    public class vMmB
    {
        public static int BtnS = 0x01;
        public static int BtnL = 0x02;
        public static int BtnX = 0x08;

        public vMmB()
        {
            id = -1;
            time = 0;
        }
        public vMmB(int id, int btns, ushort time)
        {
            this.id = id;
            this.btns = btns;
            this.time = time;
        }
        public int id;
        public int btns;
        public ushort time;
    }

    public class vMmJB
    {
        public static int BtnS = 0x01;
        public static int BtnL = 0x02;
        public static int BtnX = 0x08;
        public static int BtnJ = 0x10;

        public vMmJB()
        {
            id = -1;
            time = 0;
        }
        public vMmJB(int id, float jx, float jy, int btns, ushort time)
        {
            this.id = id;
            this.jx = jx;
            this.jy = jy;
            this.btns = btns;
            this.time = time;
        }
        public int id;
        public float jx;
        public float jy;
        public int btns;
        public ushort time;
    }

    public class vMmAGB
    {
        public static int BtnS = 0x01;
        public static int BtnL = 0x02;
        public static int BtnX = 0x08;

        public vMmAGB()
        {
            id = -1;
            time = 0;
        }
        public vMmAGB(int id, float ax, float ay, float az, float gx, float gy, float gz, ushort dtime, int btns, ushort time)
        {
            this.id = id;
            this.ax = ax;
            this.ay = ay;
            this.az = az;
            this.gx = gx;
            this.gy = gy;
            this.gz = gz;
            this.dtime = dtime;
            this.btns = btns;
            this.time = time;
        }
        public int id;
        public float ax;
        public float ay;
        public float az;
        public float gx;
        public float gy;
        public float gz;
        public ushort dtime;
        public int btns;
        public ushort time;
    }

    public class vMmAGJB
    {
        public static int BtnS = 0x01;
        public static int BtnL = 0x02;
        public static int BtnX = 0x08;
        public static int BtnJ = 0x10;

        public vMmAGJB()
        {
            id = -1;
            time = 0;
        }
        public vMmAGJB(int id, float ax, float ay, float az, float gx, float gy, float gz, ushort dtime, float jx, float jy, int btns, ushort time)
        {
            this.id = id;
            this.ax = ax;
            this.ay = ay;
            this.az = az;
            this.gx = gx;
            this.gy = gy;
            this.gz = gz;
            this.dtime = dtime;
            this.jx = jx;
            this.jy = jy;
            this.btns = btns;
            this.time = time;
        }
        public int id;
        public float ax;
        public float ay;
        public float az;
        public float gx;
        public float gy;
        public float gz;
        public ushort dtime;
        public float jx;
        public float jy;
        public int btns;
        public ushort time;
    }

    public class vMmP
    {
        public vMmP()
        {
            id = -1;
            time = 0;
        }
        public vMmP(int id, int pressure, ushort time)
        {
            this.id = id;
            this.pressure = pressure;
            this.time = time;
        }
        public int id;
        public int pressure;
        public ushort time;
    }

    // analysis_golf
    // analysis_parkgolf
    // analysis_football
    // analysis_teeball
    // analysis_bowling
    // 공의 움직임 분석정보
    //    {
    //        "Command":             // (integer) - 0:data 1:ready 2:not ready
    //        "Speed":               // (float) 공 속도 : 속력 단위 m/s
    //        "LaunchAngle":         // (float) 상하각 : (아래 - 값, 위 + 값) 단위 도 ex 360도 90도 180도
    //        "HorizontalAngle":     // (float) 좌우각 : (좌 - 값, 우 + 값) 단위 도 ex 360도 90도 180도
    // 
    //        "BackSpin":            // (float) optional 백스핀 크기 : 스핀 없을시 0값 단위 rpm
    //        "SideSpin":            // (float) optional 사이드 스핀 크기 : 스핀 없을시 0값 (좌 - 값, 우 + 값) 단위 rpm
    //        "ImpactPosition":      // (float) optional : 공이 타격한 순간 정면에서 벗어난 %정도
    //        "FaceAngleToTarget":   // (float) optional : 스크린 정면을 기준으로 해서 공을 맞는 클럽의 좌우 기울기의 각도를 말함. 
    //        "FaceAngleToPath":     // (float) optional : 공 기준으로 배트든,클럽이든 타격한 순간, 들어오면서 나가는 각도(좌 - 값, 우 + 값)
    // 
    //	      "ClubType" :           // (int) optional(골프경우만) : 클럽 타입 관련.
    //        "CSpeed" :             // (float) optional(골프경우만) : 클럽 헤드 속도(골프 채or배트가 움직이는 속도) , 속력 단위 m/s
    //        "SmashFactor":         // (float) optional(골프경우만) : 볼 속도를 클럽속도로 나눈 값 속도 단위는 m/s
    //
    //        "HPPositionX":         // (float) optional(볼링경우만) : 중앙기준에서 공의 출발지점의 x위치(mm)
    //
    //        "ClubPath":            // (string) optional : 클럽 이미지 공통 네이밍 : 이미지 폴더 경로 + 공통 네이밍ex)C:\Sensor_224_F\shot_224\shot_club_l_224\image_
    //        "BodyPath":            // (string) optional : 몸 이미지 공통 네이밍 : 이미지 폴더 경로 + 공통 네이밍 ex)C:\Swing_B\swing_
    //    }

    // analysisball
    // 공의 움직임 분석정보
    //    {
    //        "Command":             // (integer) - 0:data 1:ready 2:not ready
    //	      "ClubType" :           // (int) 클럽 타입 관련.
    //        "CSpeed" :             // (float) 클럽 헤드 속도(골프 채or배트가 움직이는 속도) , 속력 단위 m/s
    //        "Speed":               // (float) 공 속도 : 속력 단위 m/s
    //        "LaunchAngle":         // (float) 상하각 : (아래 - 값, 위 + 값) 단위 도 ex 360도 90도 180도
    //        "HorizontalAngle":     // (float) 좌우각 : (좌 - 값, 우 + 값) 단위 도 ex 360도 90도 180도
    //        "BackSpin":            // (float) 백스핀 크기 : 스핀 없을시 0값 단위 rpm
    //        "SideSpin":            // (float) 사이드 스핀 크기 : 스핀 없을시 0값 (좌 - 값, 우 + 값) 단위 rpm
    //        "ClubPath":            // (string) 클럽 이미지 공통 네이밍 : 이미지 폴더 경로 + 공통 네이밍ex)C:\Sensor_224_F\shot_224\shot_club_l_224\image_
    //        "BodyPath":            // (string) 몸 이미지 공통 네이밍 : 이미지 폴더 경로 + 공통 네이밍 ex)C:\Swing_B\swing_
    //        "SmashFactor":         // (float) : 볼 속도를 클럽속도로 나눈 값 속도 단위는 m/s
    //        "ImpactPosition":      // (float) : 공이 타격한 순간 정면에서 벗어난 %정도
    //        "FaceAngleToTarget":   // (float) : 스크린 정면을 기준으로 해서 공을 맞는 클럽의 좌우 기울기의 각도를 말함. 
    //        "FaceAngleToPath":     // (float) : 공 기준으로 배트든,클럽이든 타격한 순간, 들어오면서 나가는 각도(좌 - 값, 우 + 값)
    //    }
}
