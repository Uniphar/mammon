apiVersion: v1
kind: Service
metadata:
  name: keyrotationtool-srv
  labels:
    app: keyrotationtool-app
    service: keyrotationtool-srv
spec:
  ports:
    - name: http
      port: 80
      targetPort: {{ .Values.port }}
  selector:
    service: keyrotationtool-srv