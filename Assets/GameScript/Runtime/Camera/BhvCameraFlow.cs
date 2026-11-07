using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BhvCameraFlow : MonoBehaviour
{
	private Transform _trans;

	/// <summary>
	/// 偏移
	/// </summary>
	public Vector3 Offset = new Vector3(0, 12, 10);

	/// <summary>
	/// 跟随平滑系数（越大越跟手，0 表示瞬间跟随）
	/// </summary>
	public float FollowSharpness = 12f;

	/// <summary>
	/// 相机跟随目标
	/// </summary>
	public Transform FlowTarget;

	public Transform CameraTransform
	{
		get
		{
			return _trans;
		}
	}

	private void Awake()
	{
		_trans = this.transform;
	}
	void LateUpdate()
	{
		if (FlowTarget == null)
			return;

		Vector3 targetPos = FlowTarget.position + Offset;

		// 指数平滑：与帧率无关，避免掉帧时相机突跳
		if (FollowSharpness > 0f)
		{
			float t = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
			_trans.position = Vector3.Lerp(_trans.position, targetPos, t);
		}
		else
		{
			_trans.position = targetPos;
		}

		// Offset 为零时会退化成零向量，LookRotation 会每帧报错
		Vector3 forward = FlowTarget.position - _trans.position;
		if (forward.sqrMagnitude > 0.0001f)
			_trans.rotation = Quaternion.LookRotation(forward, Vector3.up);
	}
}
