<?php $this->includeCss('dashboard.css'); ?>
<?php $this->set('template.subtitle', $this->msg('Dashboard.about.versions', $this->get('product.name'), $this->get('product.version'))); ?>
<?php if ($this->has('versions')) : ?>
	<h2 class="versions"><?php echo $this->msg('Dashboard.about.api'); ?></h2>
	<dl class="assemblies">
	<?php foreach ($this->get('versions') as $assembly) : ?>
		<dt><?php echo htmlspecialchars($assembly['@name']); ?></dt> 
		<dd>
			(<?php echo htmlspecialchars($assembly['AssemblyVersion']); ?>) 
					
			<?php if (!empty($assembly['SvnRevision']) && !empty($assembly['SvnBranch'])): ?>
			<span class="svn"><?php echo $this->msg('Dashboard.svn', $assembly['SvnRevision'], $assembly['SvnBranch']); ?></span>
			<?php endif; ?>
			
		</dd>
	<?php endforeach; ?>
	</dl>
<?php endif; ?>
