import { Component, inject } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';



interface User {
  name: string|null;
  email: string|null;
  password: string| null;
  age: number|  null;
}

@Component({
  selector: 'app-app-phase6-reactive-form',
  imports: [ReactiveFormsModule],
  templateUrl: './app-phase6-reactive-form.component.html',
  styleUrl: './app-phase6-reactive-form.component.css',
})

export class AppPhase6ReactiveFormComponent {
private fb = inject(FormBuilder);
userForm= new FormGroup({
  name: new FormControl('test ', Validators.required),
  email: new FormControl('', [Validators.required, Validators.email]),
  password: new FormControl('', [Validators.required, Validators.minLength(6)]),
  age: new FormControl('', [Validators.required, Validators.min(18)]),
});

userForm2 = this.fb.group({
  name: ['', Validators.required],
  email: ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, Validators.minLength(6)]],
  age: ['', [Validators.required, Validators.min(18)]],
});

submitForm() {
  if (this.userForm.valid) {
    const user: User = this.userForm.value as User;
    console.log('Form submitted:', user);
  } else {
    console.log('Form is invalid');
  }}
}
