namespace UnityEngine.PostProcessing.Utilities
{
	[RequireComponent(typeof(PostProcessingController))]
	public class FocusPuller : MonoBehaviour
	{
		[SerializeField]
		private Transform _target;

		[SerializeField]
		private float _offset;

		[SerializeField]
		private float _speed = 10f;

		private PostProcessingController _controller;

		private float _velocity;

		public Transform target
		{
			get
			{
				return _target;
			}
			set
			{
				_target = value;
			}
		}

		public float offset
		{
			get
			{
				return _offset;
			}
			set
			{
				_offset = value;
			}
		}

		public float speed
		{
			get
			{
				return _speed;
			}
			set
			{
				_speed = Mathf.Max(0.01f, value);
			}
		}

		private void Start()
		{
			_controller = GetComponent<PostProcessingController>();
		}

		private void OnValidate()
		{
			speed = _speed;
		}

		private void Update()
		{
			if (!(_target == null))
			{
				float focusDistance = _controller.depthOfField.focusDistance;
				float num = Vector3.Dot(_target.position - base.transform.position, base.transform.forward);
				float deltaTime = Time.deltaTime;
				float num2 = _velocity - (focusDistance - num) * speed * speed * deltaTime;
				float num3 = 1f + speed * deltaTime;
				_velocity = num2 / (num3 * num3);
				float focusDistance2 = focusDistance + _velocity * deltaTime;
				_controller.depthOfField.focusDistance = focusDistance2;
			}
		}
	}
}
