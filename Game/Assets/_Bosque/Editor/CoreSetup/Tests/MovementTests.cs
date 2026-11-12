using Bosque.Player;
using NUnit.Framework;
using UnityEngine;
namespace Bosque.Tests
{
    public sealed class MovementTests
    {
        private GameObject world;
        private readonly Vector3 origin = new Vector3(1000, 0, 1000);
        [SetUp] public void SetUp()
        {
            world = new GameObject("MovementTestWorld");
            world.transform.position = origin;
            Box("Floor", new Vector3(0,-.5f,0), new Vector3(100,1,100));
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(world);
        private GameObject Box(string name, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(world.transform, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            Physics.SyncTransforms(); return go;
        }
        private ThirdPersonMotor Motor()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(world.transform, false);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.center = Vector3.up * .9f; cc.radius = .35f;
            cc.stepOffset = .3f; cc.skinWidth = .03f;
            var motor = go.AddComponent<ThirdPersonMotor>();
            Physics.SyncTransforms(); motor.Simulate(Vector2.zero, false, 0, .02f);
            return motor;
        }
        [Test] public void MovementUsesCameraYawAndClampsDiagonalSpeed()
        {
            var motor=Motor();
            for(int i=0;i<50;i++) motor.Simulate(Vector2.up,false,90,.02f);
            var forward=motor.transform.position-origin;
            Assert.That(forward.x,Is.InRange(4.3f,4.7f));
            Assert.That(Mathf.Abs(forward.z),Is.LessThan(.05f));
            Object.DestroyImmediate(motor.gameObject);
            motor=Motor();
            for(int i=0;i<50;i++) motor.Simulate(Vector2.one,false,0,.02f);
            var diagonal=motor.transform.position-origin; diagonal.y=0;
            Assert.That(diagonal.magnitude,Is.InRange(4.3f,4.7f));
        }
        [Test] public void SolidWallStopsTheCharacter()
        {
            Box("Wall",new Vector3(0,1,2),new Vector3(4,2,.4f));
            var motor=Motor();
            for(int i=0;i<60;i++) motor.Simulate(Vector2.up,false,0,.02f);
            Assert.That(motor.transform.position.z-origin.z,Is.InRange(1.3f,1.6f));
        }
        [Test] public void JumpCannotBeRepeatedInTheAirAndReturnsToGround()
        {
            var motor=Motor(); float peak=0;
            for(int i=0;i<30;i++) {
                motor.Simulate(Vector2.zero,true,0,.02f);
                peak=Mathf.Max(peak,motor.transform.position.y);
            }
            Assert.That(peak,Is.InRange(.8f,1.3f));
            for(int i=0;i<60;i++) motor.Simulate(Vector2.zero,false,0,.02f);
            Assert.That(motor.transform.position.y,Is.InRange(-.1f,.1f));
        }
        [Test] public void FallResetsToTheConfiguredSafePoint()
        {
            var motor=Motor();
            var safe=new GameObject("SafeRespawn"); safe.transform.SetParent(world.transform,false);
            safe.transform.localPosition=new Vector3(3,0,2); motor.SetRespawn(safe.transform);
            var cc=motor.GetComponent<CharacterController>(); cc.enabled=false;
            motor.transform.position=origin+Vector3.down*8; cc.enabled=true;
            motor.Simulate(Vector2.zero,false,0,.02f);
            Assert.That(Vector3.Distance(motor.transform.position,safe.transform.position),Is.LessThan(.1f));
        }
        [Test] public void ZeroDeltaDoesNotMoveTheCharacter()
        {
            var motor=Motor(); var start=motor.transform.position;
            motor.Simulate(Vector2.up,true,90,0);
            Assert.That(motor.transform.position,Is.EqualTo(start));
        }
        [Test] public void CameraStaysInFrontOfAnObstructingWall()
        {
            var motor=Motor();
            Box("CameraWall",new Vector3(0,1.4f,-2),new Vector3(4,4,.2f));
            var camera=new GameObject("Camera"); camera.transform.SetParent(world.transform,false);
            var rig=camera.AddComponent<ThirdPersonCamera>();
            Physics.SyncTransforms(); rig.Configure(motor.transform); rig.Step(Vector2.zero,.02f);
            Assert.That(camera.transform.position.z-origin.z,Is.GreaterThan(-1.85f));
            Assert.That(camera.transform.position.z-origin.z,Is.LessThan(-.25f));
        }
    }
}
