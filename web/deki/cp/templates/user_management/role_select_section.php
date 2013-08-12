<?php
/* @var $this DekiView */
?>

<div class="field status">
	<?php if (!$this->has('form.options.seat-status')) : ?>
		<?php echo $this->msg('Users.form.role'); ?><br/>
		<?php $this->html('form.role-select'); ?>
	<?php else : ?>
		<span class="select">
			<?php echo $this->msg('Users.form.seat-status'); ?>
		</span><br/>
		<?php
			echo DekiForm::multipleInput(
				'radio',
				'seat_status',
				$this->get('form.options.seat-status'),
				$this->get('form.seatStatus'),
				array('disabled' => $this->get('user.isOwner'))
			);
		?>
		<?php if ($this->get('user.isOwner')) : ?>
			<?php echo $this->msg('Users.form.seat-status.owner'); ?>
		<?php else : ?>
			<?php $this->html('form.role-select'); ?>
			<div id="role-operations" class="role-operations">
				<label for="select-role_id">
					<?php echo $this->msg('Users.form.label.allowedoperations'); ?>
					<span id="role-operations-text"></span>
				</label>
			</div>
		<?php endif; ?>
		
		<script type="text/javascript">
		var roleOperations = <?php $this->js('role.operations'); ?>;
		$(function() {
			var $roleOps = $('#role-operations');
			var $roles = $('#select-role_id');
			var $ops = $('#role-operations-text');
			var $seated = $('#radio-seat_status-seated'); 

			// bind event handlers
			$roles.change(updateRoleOperations).keyup(updateRoleOperations);
			$seated.click(function() {

				// enable roles
				toggleRoles(true);
			});
			$('#radio-seat_status-unseated').click(function() {
				
				// disable roles
				toggleRoles(false);
			});
			function toggleRoles(show, now) {
				if (show) {
					now ? $roleOps.show() : $roleOps.slideDown();
					$roles.removeAttr('disabled').show();
				} else {
					now ? $roleOps.hide() : $roleOps.slideUp();
					$roles.attr('disabled', 'disabled').hide();
				}
			}
			function updateRoleOperations() {
				$ops.text(roleOperations[$roles.val()]);
			}

			// init
			if (!$seated.is(':checked')) {
				toggleRoles(false, true);
			}
			updateRoleOperations();
		});
		</script>
	<?php endif; ?>
</div>
