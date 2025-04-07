Rails.application.routes.draw do
  # Define your application routes per the DSL in https://guides.rubyonrails.org/routing.html

  # Reveal health status on /up that returns 200 if the app boots with no exceptions, otherwise 500.
  # Can be used by load balancers and uptime monitors to verify that the app is live.
  get "up" => "rails/health#show", as: :rails_health_check

  # Route for fetching the top scores
  get '/high_scores/top', to: 'high_scores#top_scores'

  # Route for creating a new high score
  post '/high_scores', to: 'high_scores#create_score'

  # Route for updating an existing high score by ID
  put '/high_scores/:id', to: 'high_scores#update_score'

  # Route for deleting a high score by ID
  delete '/high_scores/:id', to: 'high_scores#destroy_score'
end