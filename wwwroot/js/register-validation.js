(function() {
	// Validation functions
	function validateEmail(value) {
		var emailRegex = new RegExp("^[a-z0-9._%+\\-]+@[a-z0-9.\\-]+\\.[a-z]{2,}$", "i");
		return emailRegex.test(value);
	}

	function validateName(value) {
		var nameRegex = new RegExp("^[a-zA-Z\\s'\\-]+$");
		return nameRegex.test(value) && value.length > 0;
	}

	function validateNric(value) {
		var nricRegex = new RegExp("^[a-zA-Z0-9\\-]+$");
		return nricRegex.test(value) && value.length > 0;
	}

	function validatePassword(value) {
		return value.length >= 12;
	}

	function validateConfirmPassword(value) {
		var password = document.getElementById('password').value;
		return value === password && value.length > 0;
	}

	function validateDateOfBirth(value) {
		if (!value) return false;
		var date = new Date(value);
		var today = new Date();
		var age = today.getFullYear() - date.getFullYear();
		var monthDiff = today.getMonth() - date.getMonth();
		var dayDiff = today.getDate() - date.getDate();
		
		if (monthDiff < 0 || (monthDiff === 0 && dayDiff < 0)) {
			return age - 1 >= 16;
		}
		return age >= 16 && date < today;
	}

	function validateFile(fileInput) {
		if (!fileInput.files || fileInput.files.length === 0) return true;
		var file = fileInput.files[0];
		var maxSize = 5 * 1024 * 1024;
		var allowedExtensions = ['.pdf', '.docx', '.doc'];
		var fileName = file.name.toLowerCase();
		var ext = fileName.substring(fileName.lastIndexOf('.'));
		return file.size <= maxSize && allowedExtensions.indexOf(ext) >= 0;
	}

	function validateText(value) {
		return value.length <= 5000;
	}

	function getValidator(validationType) {
		var validators = {
			email: validateEmail,
			name: validateName,
			nric: validateNric,
			password: validatePassword,
			confirmPassword: validateConfirmPassword,
			dateOfBirth: validateDateOfBirth,
			file: validateFile,
			text: validateText,
			required: function(val) { return val && val.trim().length > 0; }
		};
		return validators[validationType];
	}

	var formInputs = document.querySelectorAll('[data-validate]');

	function validateField(element) {
		var validateType = element.dataset.validate;
		var value = element.value || '';
		var fieldName = element.name;
		var formGroup = element.closest('.mb-3');
		var errorSpan = formGroup.querySelector('span[class*="text-danger"]');
		var hintSpan = formGroup.querySelector('small[id$="-hint"]');

		var isValid = true;

		var validator = getValidator(validateType);
		if (validator) {
			if (validateType === 'file') {
				isValid = validator(element);
			} else {
				isValid = validator(value);
			}
		}

		if (!isValid && value !== '') {
			element.classList.remove('is-valid');
			element.classList.add('is-invalid', 'border-danger');
			
			if (errorSpan && !errorSpan.textContent) {
				errorSpan.textContent = getErrorMessage(validateType, fieldName);
			}
			if (hintSpan) {
				hintSpan.classList.remove('d-none');
			}
		} else if (isValid && value !== '') {
			element.classList.remove('is-invalid', 'border-danger');
			element.classList.add('is-valid', 'border-success');
			if (errorSpan) {
				errorSpan.textContent = '';
			}
			if (hintSpan) {
				hintSpan.classList.add('d-none');
			}
		} else if (value === '') {
			element.classList.remove('is-valid', 'is-invalid', 'border-success', 'border-danger');
			if (errorSpan) {
				errorSpan.textContent = '';
			}
			if (hintSpan) {
				hintSpan.classList.add('d-none');
			}
		}

		return isValid || value === '';
	}

	function getErrorMessage(type, fieldName) {
		var messages = {
			required: fieldName + ' is required',
			email: 'Please enter a valid email address',
			name: 'Only letters, spaces, hyphens and apostrophes allowed',
			nric: 'Only alphanumeric and hyphens allowed',
			password: 'Password must be at least 12 characters',
			confirmPassword: 'Passwords do not match',
			dateOfBirth: 'You must be at least 16 years old',
			file: 'File must be PDF or DOCX, max 5MB',
			text: 'Text is too long (max 5000 characters)'
		};
		return messages[type] || 'Invalid input';
	}

	formInputs.forEach(function(input) {
		input.addEventListener('blur', function() {
			validateField(input);
		});
		input.addEventListener('input', function() {
			validateField(input);
		});
		input.addEventListener('change', function() {
			validateField(input);
		});
	});

	var password = document.getElementById('password');
	var confirmPassword = document.getElementById('confirmPassword');
	var submitBtn = document.getElementById('submitBtn');
	var strengthBadge = document.getElementById('passwordStrength');
	var rules = {
		length: document.getElementById('rule-length'),
		lower: document.getElementById('rule-lower'),
		upper: document.getElementById('rule-upper'),
		digit: document.getElementById('rule-digit'),
		special: document.getElementById('rule-special')
	};

	function testPassword(pw) {
		return {
			length: pw.length >= 12,
			lower: /[a-z]/.test(pw),
			upper: /[A-Z]/.test(pw),
			digit: /\d/.test(pw),
			special: /[^A-Za-z0-9]/.test(pw)
		};
	}

	function updateUI() {
		var pw = password.value || '';
		var results = testPassword(pw);
		var met = 0;
		for (var k in results) {
			if (results[k]) {
				met++;
				rules[k].classList.remove('text-muted');
				rules[k].classList.add('text-success');
			} else {
				rules[k].classList.remove('text-success');
				rules[k].classList.add('text-muted');
			}
		}

		if (met === 5) {
			strengthBadge.textContent = 'Password strength: STRONG';
			strengthBadge.className = 'badge bg-success small';
			submitBtn.disabled = false;
		} else if (met >= 3) {
			strengthBadge.textContent = 'Password strength: MEDIUM';
			strengthBadge.className = 'badge bg-warning text-dark small';
			submitBtn.disabled = true;
		} else {
			strengthBadge.textContent = 'Password strength: WEAK';
			strengthBadge.className = 'badge bg-danger small';
			submitBtn.disabled = true;
		}

		if (confirmPassword.value) {
			validateField(confirmPassword);
		}
	}

	var togglePassword = document.getElementById('togglePassword');
	var toggleConfirmPassword = document.getElementById('toggleConfirmPassword');

	function toggleInputVisibility(inputEl, toggleBtn, eyeOpenId, eyeClosedId) {
		if (!inputEl || !toggleBtn) return;
		var eyeOpen = document.getElementById(eyeOpenId);
		var eyeClosed = document.getElementById(eyeClosedId);
		if (inputEl.type === 'password') {
			inputEl.type = 'text';
			if (eyeOpen) eyeOpen.style.display = 'none';
			if (eyeClosed) eyeClosed.style.display = '';
		} else {
			inputEl.type = 'password';
			if (eyeOpen) eyeOpen.style.display = '';
			if (eyeClosed) eyeClosed.style.display = 'none';
		}
	}

	if (togglePassword) {
		togglePassword.addEventListener('click', function() {
			toggleInputVisibility(password, togglePassword, 'passwordEyeOpen', 'passwordEyeClosed');
		});
	}
	if (toggleConfirmPassword) {
		toggleConfirmPassword.addEventListener('click', function() {
			toggleInputVisibility(confirmPassword, toggleConfirmPassword, 'confirmEyeOpen', 'confirmEyeClosed');
		});
	}

	password.addEventListener('input', updateUI);
	confirmPassword.addEventListener('input', updateUI);
	
	updateUI();
})();
