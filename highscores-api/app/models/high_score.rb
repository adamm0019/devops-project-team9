class HighScore < ApplicationRecord
     validates :name,
                presence: true,
                length: { maximum: 50 },
                format: {
                  with: /\A[\w\s'-]+\z/,
                  message: "only allows letters, numbers, spaces, apostrophes, and hyphens"
                }

      validates :game,
                presence: true,
                length: { maximum: 50 },
                format: {
                  with: /\A[a-zA-Z0-9_]+\z/,
                  message: "only allows letters, numbers, and underscores"
                }

      validates :score,
                presence: true,
                numericality: {
                  only_integer: true,
                  greater_than_or_equal_to: 0
                }
end