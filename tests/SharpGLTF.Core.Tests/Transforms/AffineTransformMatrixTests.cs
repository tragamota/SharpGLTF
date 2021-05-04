﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Transforms
{
    [Category("Core.Transforms")]
    public class AffineTransformMatrixTests
    {
        [TestCase(0, 0, 0, 0, 0, 0)]
        [TestCase(1, 2, 4, 3, 2, 1)]
        [TestCase(-1, 1, 3, 2, 0, 1)]
        [TestCase(0, 0, 1, 0, 1, 0)]
        [TestCase(0, -1, 1, -2, 1, 0)]
        public void CalculateInverseBindMatrix(float mx, float my, float mz, float jx, float jy, float jz)
        {
            var model = Matrix4x4.CreateFromYawPitchRoll(mx, my, mz);            
            var joint = Matrix4x4.CreateFromYawPitchRoll(jx, jy, jz);
            joint.Translation = new Vector3(jx, jy, jz);

            var invBindMatrix = SkinnedTransform.CalculateInverseBinding(model, joint);

            Matrix4x4.Invert(model, out Matrix4x4 xform);            
            Matrix4x4.Invert(joint * xform, out Matrix4x4 result);
            NumericsAssert.AreEqual(result, invBindMatrix, 0.000001f);

            Matrix4x4.Invert(joint, out Matrix4x4 invJoint);
            result = model * invJoint;
            NumericsAssert.AreEqual(result, invBindMatrix, 0.000001f);
        }


        [Test]
        public void TestMatrixNormalization()
        {
            void testMatrix(Matrix4x4 m, float tolerance = 0)
            {
                var o = m;

                Matrix4x4Factory.NormalizeMatrix(ref m);

                NumericsAssert.AreEqual(o, m, tolerance);

                Assert.IsTrue(Matrix4x4.Decompose(m, out _, out _, out _));
                Assert.IsTrue(Matrix4x4.Invert(m, out _));
            }

            void testSkewed(Func<Matrix4x4, Matrix4x4> mf, float tolerance = 0)
            {
                var m = Matrix4x4.Identity;

                var o = m = mf(m);

                Assert.IsFalse(Matrix4x4.Decompose(m, out _, out _, out _));

                Matrix4x4Factory.NormalizeMatrix(ref m);

                NumericsAssert.AreEqual(o, m, tolerance);                

                Assert.IsTrue(Matrix4x4.Decompose(m, out _, out _, out _));
                Assert.IsTrue(Matrix4x4.Invert(m, out _));               
            }
            
            testSkewed(m => { m.M12 += 0.34f; return m; }, 0.34f);
            testSkewed(m => { m.M13 += 0.34f; return m; }, 0.34f);
            testSkewed(m => { m.M21 += 0.34f; return m; }, 0.34f);
            testSkewed(m => { m.M23 += 0.34f; return m; }, 0.34f);
            testSkewed(m => { m.M31 += 0.34f; return m; }, 0.34f);
            testSkewed(m => { m.M32 += 0.34f; return m; }, 0.34f);

            testSkewed(m => { m.M12 += 0.1f; m.M23 -= 0.1f; m.M31 += 0.05f; return m; }, 0.20f);

            // test normalization with uneven scaling

            testMatrix(Matrix4x4.CreateScale(0.0001f) * Matrix4x4.CreateFromYawPitchRoll(1, 2, 3), 0.0001f);
            testMatrix(Matrix4x4.CreateScale(1000) * Matrix4x4.CreateFromYawPitchRoll(1, 2, 3), 0.0001f);

            var SxR = Matrix4x4.CreateScale(5, 1, 1) * Matrix4x4.CreateFromYawPitchRoll(1, 2, 3);   // Decomposable
            var RxS = Matrix4x4.CreateFromYawPitchRoll(1, 2, 3) * Matrix4x4.CreateScale(5, 1, 1);   // Not Decomposable            

            Assert.IsTrue(Matrix4x4.Decompose(SxR, out _, out _, out _));
            testMatrix(SxR, 0.0001f);

            Assert.IsFalse(Matrix4x4.Decompose(RxS, out _, out _, out _));
            testMatrix(RxS, 100);           
        }

        [Test]
        public void TestAffineTransformIdentity()
        {
            var asMatrix = new AffineTransform(Matrix4x4.Identity);
            var asDecomposed = new AffineTransform(null, null, null);

            NumericsAssert.AreEqual(Matrix4x4.Identity, asMatrix.Matrix);
            NumericsAssert.AreEqual(Matrix4x4.Identity, asDecomposed.Matrix);

            Assert.IsTrue(asMatrix.IsIdentity);
            Assert.IsTrue(asDecomposed.IsIdentity);
        }

        [Test]
        public void TestAffineTransformMult()
        {
            var s_a = new Vector3(1, 2, 4);
            var r_a = Quaternion.CreateFromYawPitchRoll(1, 2, 3);
            var t_a = Vector3.Zero;

            var s_b = new Vector3(1, 1, 1);
            var r_b = Quaternion.CreateFromYawPitchRoll(1, 0, 2);
            var t_b = new Vector3(3, -4, 2);

            var mat_a = Matrix4x4.CreateScale(s_a) * Matrix4x4.CreateFromQuaternion(r_a) * Matrix4x4.CreateTranslation(t_a);
            var mat_b = Matrix4x4.CreateScale(s_b) * Matrix4x4.CreateFromQuaternion(r_b) * Matrix4x4.CreateTranslation(t_b);
            var mat_ab = Matrix4x4.Multiply(mat_a, mat_b);
            var mat_ba = Matrix4x4.Multiply(mat_b, mat_a);

            var srt_a = new AffineTransform(s_a, r_a, t_a);
            var srt_b = new AffineTransform(s_b, r_b, t_b);
            var srt_ab = AffineTransform.Multiply(srt_a, srt_b);
            var srt_ba = AffineTransform.Multiply(srt_b, srt_a);

            NumericsAssert.AreEqual(mat_ab, srt_ab.Matrix, 0.0001f);
            NumericsAssert.AreEqual(mat_ba, srt_ba.Matrix, 0.0001f);
        }
    }    
}