
<form method="post" action="<?php $this->html('form.action'); ?>">
 <input type="text" name="searchterm" value="<?php $this->html('searchterm'); ?>"/> 

 <button name="submit" value="submit" class="submit">search</button>
</form>

<?php $this->html('contents'); ?>
<?php $this->html('comments'); ?>

